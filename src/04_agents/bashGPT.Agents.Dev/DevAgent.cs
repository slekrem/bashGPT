using System.Diagnostics;
using System.Text;
using bashGPT.Agents;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev;

/// <summary>
/// Specialized development agent with access to filesystem, git, build, and test tools.
/// </summary>
public sealed class DevAgent : AgentBase
{
    private readonly string _workingDirectory = Directory.GetCurrentDirectory();

    public override string Id => "dev";

    public override string Name => "Dev-Agent";

    /// <summary>
    /// Working directory captured at agent startup. Ensures all tool calls operate in the
    /// same directory regardless of later process CWD changes.
    /// </summary>
    public override string? WorkingDirectory => _workingDirectory;

    // Context tools are owned directly — no registry needed.
    public override IReadOnlyList<ITool> GetOwnedTools() =>
    [
        new ContextLoadFilesTool(),
        new ContextUnloadFilesTool(),
        new ContextClearFilesTool(),
    ];

    // Registry tools are resolved via the plugin system at runtime.
    public override IReadOnlyList<string> EnabledTools =>
    [
        .. base.EnabledTools,   // owned: context_load_files, context_unload_files, context_clear_files
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

    public override IReadOnlyList<string> SystemPrompt => GetSystemPrompt(null);

    public override IReadOnlyList<string> GetSystemPrompt(string? sessionPath = null) =>
    [
        BuildRolePrompt(),
        BuildToolCallRulesPrompt(),
        BuildGitContext(),
        BuildFileExplorerContext(),
        BuildEditorContext(sessionPath),
    ];

    private static string BuildRolePrompt() =>
        """
        You are an experienced software engineer working inside a local dev environment.
        Solve tasks step by step through focused, minimal tool usage — read before you write.
        Prefer small, targeted changes over large rewrites. Never guess file contents.

        You have an Editor: use 'context_load_files' to open files into it before working on them.
        The Editor always reflects the latest file content and shows git diffs automatically on each request.
        Use 'context_unload_files' to close files you no longer need, and 'context_clear_files' to reset.
        """;

    private static string BuildToolCallRulesPrompt() =>
        """
        Tool call rules — follow strictly, no exceptions:
        - Match the schema exactly: correct types, all required fields, valid values.
        - Never invent or omit fields; use the schema as the single source of truth.
        - "missing_required_field": add the named field and retry.
        - "invalid_type" / "invalid_value": correct only the named field and retry.
        - "invalid_json": send well-formed JSON and retry.
        - Missing optional path: default to "path": ".".
        - Do not retry more than once per error; escalate if the same error repeats.
        """;

    /// <summary>
    /// Builds a git context: branch, upstream, recent commits, and working tree status.
    /// </summary>
    private static string BuildGitContext()
    {
        var branch = Git("rev-parse --abbrev-ref HEAD");
        if (branch is null) return string.Empty;

        var sb = new StringBuilder("# Git Context\n\n");

        var upstream = Git("rev-parse --abbrev-ref --symbolic-full-name @{u}");
        sb.AppendLine(upstream is not null
            ? $"**Branch:** `{branch}` → `{upstream}`"
            : $"**Branch:** `{branch}` (no upstream)");

        var ahead  = Git("rev-list --count @{u}..HEAD 2>/dev/null");
        var behind = Git("rev-list --count HEAD..@{u} 2>/dev/null");
        if (ahead is not null && behind is not null)
            sb.AppendLine($"**Sync:** {ahead} ahead, {behind} behind");

        var log = Git("log -5 --oneline");
        if (log is not null)
        {
            sb.AppendLine("\n**Recent commits:**");
            foreach (var line in log.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine($"- `{line}`");
        }

        var status = Git("status --short");
        if (status is not null)
        {
            sb.AppendLine("\n**Working tree:**");
            sb.AppendLine("```");
            sb.AppendLine(status);
            sb.Append("```");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds a directory tree of all git-tracked and untracked files.
    /// Untracked files are marked with (*).
    /// </summary>
    private static string BuildFileExplorerContext()
    {
        var tracked   = Git("ls-files")?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var untracked = Git("ls-files --others --exclude-standard")?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? [];

        if (tracked.Length == 0 && untracked.Length == 0) return string.Empty;

        var untrackedSet = new HashSet<string>(untracked);
        var root = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in tracked.Concat(untracked).Distinct().Order())
        {
            var parts = path.Split('/');
            InsertIntoTree(root, parts, 0, untrackedSet.Contains(path));
        }

        var sb = new StringBuilder("# File Explorer\n\n```\n");
        RenderTree(sb, root, "");
        sb.Append("```");
        if (untrackedSet.Count > 0)
            sb.Append("\n\n`*` = untracked (not yet staged)");
        return sb.ToString();
    }

    private static void InsertIntoTree(SortedDictionary<string, object?> node, string[] parts, int depth, bool untracked)
    {
        if (depth == parts.Length - 1)
        {
            node[untracked ? parts[depth] + " *" : parts[depth]] = null;
            return;
        }
        var dir = parts[depth] + "/";
        if (!node.TryGetValue(dir, out var child) || child is not SortedDictionary<string, object?>)
        {
            child = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            node[dir] = child;
        }
        InsertIntoTree((SortedDictionary<string, object?>)child, parts, depth + 1, untracked);
    }

    private static void RenderTree(StringBuilder sb, SortedDictionary<string, object?> node, string prefix)
    {
        var entries = node.ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            var isLast = i == entries.Count - 1;
            sb.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{entries[i].Key}");
            if (entries[i].Value is SortedDictionary<string, object?> child)
                RenderTree(sb, child, prefix + (isLast ? "    " : "│   "));
        }
    }

    /// <summary>
    /// Builds the Editor context: current file contents plus git diff for each loaded file.
    /// File contents are re-read from disk on every request so changes are always current.
    /// </summary>
    private static string BuildEditorContext(string? sessionPath = null)
    {
        var paths = ContextFileCache.ReadFiles(sessionPath);
        if (paths.Count == 0) return string.Empty;

        var sb = new StringBuilder("# Editor\n\n");
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

                var diff = Git($"diff HEAD -- \"{path}\"");
                if (diff is not null)
                {
                    sb.AppendLine("\n**Changes since last commit:**");
                    sb.AppendLine("```diff");
                    sb.AppendLine(diff);
                    sb.AppendLine("```");
                }
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

    protected override string GetAgentMarkdown(string? sessionPath = null) =>
        string.Join("\n\n---\n\n", GetSystemPrompt(sessionPath).Where(s => !string.IsNullOrWhiteSpace(s)));
}
