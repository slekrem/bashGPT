using System.Diagnostics;
using System.Text;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using BashGPT.Agents;

namespace BashGPT.Agents.Dev;

/// <summary>
/// Specialized development agent with access to filesystem, git, build, and test tools.
/// </summary>
public sealed class DevAgent : AgentBase
{
    public override string Id => "dev";

    public override string Name => "Dev-Agent";

    public override IReadOnlyList<string> EnabledTools =>
    [
        "fetch",
        "filesystem_read",
        "filesystem_write",
        "filesystem_search",
        "git_status",
        "git_diff",
        "git_log",
        "git_branch",
        "git_add",
        "git_commit",
        "git_checkout",
        "test_run",
        "build_run",
        "shell_exec",
        "context_load_files",
        "context_unload_files",
        "context_clear_files",
    ];

    public override AgentLlmConfig LlmConfig => new(
        Temperature:       0.1,    // deterministic: code is not creative output
        TopP:              0.95,
        NumCtx:            65536,  // 64k context for files, diffs, and logs
        MaxTokens:         8192,   // enough output room for complex coding tasks
        ReasoningEffort:   "high", // complex tasks benefit from strong reasoning
        FrequencyPenalty:  0.1,    // dampen repetitive tool-call loops
        ParallelToolCalls: false,  // sequential is safer for file mutations
        Stream:            true
    );

    public override IReadOnlyList<string> SystemPrompt =>
    [
        """
        You are an experienced software engineer. Solve tasks through focused tool usage.
        Before working on a task, load relevant source files into context with 'context_load_files'.
        """,
        """
        Tool call rules:
        - Follow the schema strictly: correct types, all required fields, valid values.
        - If a tool fails with "missing_required_field", add exactly that field and retry.
        - If a tool fails with "invalid_type" or "invalid_value", correct only the named field.
        - If a tool fails with "invalid_json", send valid JSON and retry.
        - For missing optional paths, use "path": "." as the default.
        """,
        BuildProjectContext(),
        BuildLoadedFilesContext(),
    ];

    /// <summary>
    /// Builds project context at runtime: git info plus all tracked files.
    /// Ignored files (.gitignore) are omitted. Rebuilt fresh for every chat request.
    /// </summary>
    private static string BuildProjectContext()
    {
        var cwd = Directory.GetCurrentDirectory();
        var sb  = new StringBuilder("# Project Context\n\n");

        sb.AppendLine($"**Directory:** `{cwd}`\n");
        var branch     = Git("rev-parse --abbrev-ref HEAD");
        var lastCommit = Git("log -1 --oneline");
        if (branch is not null)
        {
            sb.AppendLine("**Git:**");
            sb.AppendLine($"- Branch: `{branch}`");
            if (lastCommit is not null)
                sb.AppendLine($"- Last commit: `{lastCommit}`");
            sb.AppendLine();
        }

        var files = Git("ls-files");
        if (files is not null)
        {
            var grouped = files
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .GroupBy(f => f.Contains('/') ? f[..f.IndexOf('/')] : ".")
                .OrderBy(g => g.Key);

            sb.AppendLine("**Files (git tracked):**");
            foreach (var group in grouped)
            {
                sb.AppendLine($"\n`{group.Key}/`");
                foreach (var file in group.Order())
                    sb.AppendLine($"  - `{file}`");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Loads all files stored in the cache and returns their content as formatted text.
    /// Empty strings are filtered automatically by ServerChatRunner.
    /// </summary>
    private static string BuildLoadedFilesContext()
    {
        var paths = ContextFileCache.ReadFiles();
        if (paths.Count == 0) return string.Empty;

        var sb = new StringBuilder("# Loaded Files\n\n");
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > 131_072)
                {
                    sb.AppendLine($"## `{path}`\n\n> File too large ({info.Length / 1024} KB), skipped.\n");
                    continue;
                }

                sb.Append(ContextFileCache.FormatFileBlock(path, File.ReadAllText(path)));
            }
            catch (Exception ex)
            {
                sb.AppendLine($"## `{path}`\n\n> Read error: {ex.Message}\n");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string? Git(string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            return proc?.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }

    protected override string GetAgentMarkdown() => """
        # Dev-Agent

        Specialized software development agent with full access to filesystem, git, build, and testing tools.

        ## Capabilities

        - Read, write, and search files
        - Git operations (status, diff, log, branch, commit, checkout)
        - Run builds and evaluate test results
        - Execute shell commands
        - Fetch web content

        ## Enabled Tools

        | Tool | Description |
        |---|---|
        | `fetch` | Fetch web content |
        | `filesystem_read` | Read files and directories |
        | `filesystem_write` | Create and edit files |
        | `filesystem_search` | Search files by pattern |
        | `git_status` | Show git status |
        | `git_diff` | Compare changes |
        | `git_log` | Inspect commit history |
        | `git_branch` | Manage branches |
        | `git_add` | Stage changes |
        | `git_commit` | Create commits |
        | `git_checkout` | Switch branches |
        | `test_run` | Run tests |
        | `build_run` | Start a build |
        | `shell_exec` | Execute shell commands |

        ## Notes

        This agent follows strict tool-call rules and automatically retries invalid calls with corrected arguments.
        """;
}
