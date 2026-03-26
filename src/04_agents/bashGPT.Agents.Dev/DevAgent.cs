using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using bashGPT.Agents.Dev.Tools;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev;

/// <summary>
/// Specialized development agent with access to filesystem, git, build, and test tools.
/// </summary>
public sealed partial class DevAgent : AgentBase
{
    private readonly string _workingDirectory = Directory.GetCurrentDirectory();

    public override string Id => "dev";

    public override string Name => "Dev-Agent";

    /// <summary>
    /// Working directory captured at agent startup. Ensures all tool calls operate in the
    /// same directory regardless of later process CWD changes.
    /// </summary>
    public override string? WorkingDirectory => _workingDirectory;

    // All tools are owned directly — no registry needed.
    public override IReadOnlyList<ITool> GetOwnedTools() =>
    [
        new ReadFileTool(_workingDirectory),
        new WriteFileTool(_workingDirectory),
        new EditFileTool(_workingDirectory),
        new SearchTool(_workingDirectory),
        new GhPrCreateTool(_workingDirectory),
        new GhCommentTool(_workingDirectory),
        new GitCommitTool(_workingDirectory),
    ];

    // Registry tools are resolved via the plugin system at runtime.
    public override IReadOnlyList<string> EnabledTools =>
    [
        .. base.EnabledTools,   // owned: read_file, write_file, edit_file, search, gh_pr_create, gh_comment, git_commit
    ];

    public override AgentLlmConfig LlmConfig => new(
        Temperature:       0.1,    // deterministic: code is not creative output
        TopP:              0.95,
        NumCtx:            65536,  // 64k context for files, diffs, and logs
        MaxTokens:         8192,   // enough output room for complex coding tasks
        ReasoningEffort:   "medium", // "high" causes reasoning loops in Ollama >= 0.12.4 (ollama/ollama#12606)
        FrequencyPenalty:  0.1,    // dampen repetitive tool-call loops
        ParallelToolCalls: false,  // sequential is safer for file mutations
        Stream:            true
    );

    public override IReadOnlyList<string> SystemPrompt => GetSystemPrompt(null);

    public override IReadOnlyList<string> GetSystemPrompt(string? sessionPath = null) =>
    [
        BuildRolePrompt(),
    ];

    public override IReadOnlyList<string> GetContextMessages(string? sessionPath = null) =>
    [
        BuildGitContext(_workingDirectory),
        BuildGitHubContext(_workingDirectory),
        BuildFileExplorerContext(_workingDirectory),
    ];

    private static string BuildRolePrompt() =>
        """
        You are an experienced, proactive software engineer working inside a local dev environment.
        When given a task, you complete it fully and independently — without asking for confirmation
        at every step. You read what you need, make the change, and report what you did.

        ## How to approach a task
        1. Read the context messages carefully first — Git Context, GitHub Context, File Explorer.
           Most orientation questions (branch, issue, open files) are already answered there.
        2. Search or read the relevant files to understand the existing code.
        3. Make all necessary changes — implement, fix, refactor, or extend as needed.
        4. Spot and address related issues proactively: missing tests, broken references,
           inconsistent naming, or anything else that is clearly part of the task.
        5. Report concisely what you did and why. If you notice other problems outside the task
           scope, mention them briefly — but do not fix them unless asked.

        Do NOT ask for permission before reading files or making changes.
        Do NOT wait for confirmation between steps.
        Do NOT stop halfway and ask "should I continue?" — just continue.

        ## Context (injected as user messages before this conversation)
        At the start of every request you receive up to three context messages.
        Read them carefully BEFORE calling any tool — most questions can be answered from context alone.

        '# Git Context'
          Current branch, upstream sync, divergence to main, recent commits, working tree status,
          staged and unstaged diff stats. Use this to understand what has changed and on which branch.

        '# GitHub Context'
          The GitHub issue linked to this branch AND the open pull request (if any), including review
          decision and CI check results. GitHub issues are NOT files — do not try to read them with
          read_file. The full issue description is already here in this message.

        '# File Explorer'
          A full directory tree of every git-tracked and untracked file in the repository.
          Use this to find exact file paths. Do not guess paths and do not search for files.

        ## Available tools
        You have exactly FOUR tools: 'read_file', 'write_file', 'edit_file', and 'search'.
        Do NOT call any other tool — there are no other tools available.
        Do NOT invent parameters — use only the schemas below.

        read_file  — reads one or more files and returns their content.
          {"paths": ["src/Foo.cs", "src/Bar.cs"]}

        write_file — writes the COMPLETE content of a single file (creates or overwrites).
          {"path": "src/Foo.cs", "content": "... full file content ..."}
          Use this to create new files or when rewriting most of the file.
          Always write the complete file — partial content will truncate the file.

        edit_file  — replaces an exact string in an existing file with a new string.
          {"path": "src/Foo.cs", "old_string": "... exact text ...", "new_string": "... replacement ..."}
          old_string must match exactly once — add surrounding context if needed to make it unique.
          new_string must use the exact same indentation (spaces or tabs) as the surrounding code.
          Include enough lines in old_string so the insertion point is unambiguous.
          Prefer this over write_file for small, targeted changes.
          Always read the file first so old_string matches the actual content exactly.

        search     — searches for a text string across files and returns matching lines with file:line.
          {"query": "BuildGitContext"}
          {"query": "BuildGitContext", "path": "src", "max_results": 20}

        gh_pr_create — creates a GitHub pull request for the current branch.
          {"title": "feat: my feature", "body": "## Summary\n...", "draft": false}
          Only call this when explicitly asked to create a PR or when the task is fully complete.

        gh_comment   — adds a comment to a GitHub issue or PR.
          {"number": 42, "body": "Done — implemented in commit abc123.", "type": "issue"}
          type is either "issue" or "pr" (defaults to "issue").

        git_commit   — stages files and creates a commit.
          {"message": "feat: add stash support to git context"}
          {"message": "fix: correct indentation", "paths": ["src/Foo.cs", "src/Bar.cs"]}
          Use conventional commit format. If paths is omitted, all changes are staged.

        ## How to read files
        Only call read_file when you actually need to see file contents.
        Call read_file once per file — the result stays in your conversation history.
        Before calling read_file, check whether you have already read that file earlier in this conversation.
        If you find the file content in your history, use it — do NOT read the file again.
        """;

    /// <summary>
    /// Builds a git context: branch, upstream/base divergence, recent commits,
    /// working tree status, and diff stats for staged and unstaged changes.
    /// </summary>
    private static string BuildGitContext(string workingDirectory)
    {
        var branch = Git("rev-parse --abbrev-ref HEAD", workingDirectory);
        if (branch is null) return string.Empty;

        var sb = new StringBuilder("# Git Context\n\n");

        // Branch + upstream sync
        var upstream = Git("rev-parse --abbrev-ref --symbolic-full-name @{u}", workingDirectory);
        sb.AppendLine(upstream is not null
            ? $"**Branch:** `{branch}` → `{upstream}`"
            : $"**Branch:** `{branch}` (no upstream)");

        if (upstream is not null)
        {
            var ahead  = Git("rev-list --count @{u}..HEAD", workingDirectory);
            var behind = Git("rev-list --count HEAD..@{u}", workingDirectory);
            if (ahead is not null && behind is not null)
                sb.AppendLine($"**Sync:** {ahead} ahead, {behind} behind upstream");
        }

        // Divergence from default branch (main/master), if different from upstream
        var defaultBranch = Git("rev-parse --abbrev-ref origin/HEAD", workingDirectory)
            ?.Replace("origin/", "");
        if (defaultBranch is not null && defaultBranch != branch)
        {
            var aheadBase  = Git($"rev-list --count origin/{defaultBranch}..HEAD", workingDirectory);
            var behindBase = Git($"rev-list --count HEAD..origin/{defaultBranch}", workingDirectory);
            if (aheadBase is not null && behindBase is not null)
                sb.AppendLine($"**vs `{defaultBranch}`:** {aheadBase} ahead, {behindBase} behind");
        }

        // Recent commits
        var log = Git("log -5 --oneline", workingDirectory);
        if (log is not null)
        {
            sb.AppendLine("\n**Recent commits:**");
            foreach (var line in log.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine($"- `{line}`");
        }

        // Stash entries
        var stashList = Git("stash list", workingDirectory);
        if (!string.IsNullOrWhiteSpace(stashList))
        {
            sb.AppendLine("\n**Stash entries:**");
            sb.AppendLine("```");
            sb.AppendLine(stashList);
            sb.AppendLine("```");
        }

        // Merge/Rebase status
        var mergeHead  = Git("rev-parse -q --verify MERGE_HEAD",  workingDirectory);
        var rebaseHead = Git("rev-parse -q --verify REBASE_HEAD", workingDirectory);
        if (!string.IsNullOrWhiteSpace(mergeHead))
        {
            sb.AppendLine("\n**Merge in progress:**");
            sb.AppendLine("```");
            sb.AppendLine(mergeHead);
            sb.AppendLine("```");
        }
        else if (!string.IsNullOrWhiteSpace(rebaseHead))
        {
            sb.AppendLine("\n**Rebase in progress:**");
            sb.AppendLine("```");
            sb.AppendLine(rebaseHead);
            sb.AppendLine("```");
        }

        // Working tree status (XY flags)
        var status = Git("status --short", workingDirectory);
        if (!string.IsNullOrWhiteSpace(status))
        {
            sb.AppendLine("\n**Working tree:**");
            sb.AppendLine("```");
            sb.AppendLine(status);
            sb.AppendLine("```");
        }

        // Staged changes (diff stat)
        var stagedStat = Git("diff --cached --stat", workingDirectory);
        if (!string.IsNullOrWhiteSpace(stagedStat))
        {
            sb.AppendLine("**Staged changes:**");
            sb.AppendLine("```");
            sb.AppendLine(stagedStat);
            sb.AppendLine("```");
        }

        // Unstaged changes (diff stat)
        var unstagedStat = Git("diff --stat", workingDirectory);
        if (!string.IsNullOrWhiteSpace(unstagedStat))
        {
            sb.AppendLine("**Unstaged changes:**");
            sb.AppendLine("```");
            sb.AppendLine(unstagedStat);
            sb.AppendLine("```");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds a GitHub context using the gh CLI (if available).
    /// Shows the linked issue (parsed from the branch name), the open PR with review
    /// decision and CI checks, and inline review comments.
    /// Returns an empty string when gh is not installed or not authenticated.
    /// </summary>
    private static string BuildGitHubContext(string workingDirectory)
    {
        if (!IsGhAvailable(workingDirectory)) return string.Empty;

        var sb = new StringBuilder("# GitHub Context\n\n");
        var hasContent = false;

        // Linked issue — infer number from branch name (e.g. "248-feat-foo" → #248)
        var branch = Git("rev-parse --abbrev-ref HEAD", workingDirectory);
        var issueNumber = branch is not null
            ? LeadingDigits().Match(branch).Value
            : null;

        if (!string.IsNullOrEmpty(issueNumber))
        {
            var issueJson = Gh($"issue view {issueNumber} --json number,title,state,body,labels", workingDirectory);
            if (issueJson is not null)
            {
                try
                {
                    var issue = JsonDocument.Parse(issueJson).RootElement;
                    var iTitle  = issue.TryGetProperty("title",  out var it) ? it.GetString() : null;
                    var iState  = issue.TryGetProperty("state",  out var is_) ? is_.GetString() : null;
                    var iBody   = issue.TryGetProperty("body",   out var ib) ? ib.GetString() : null;
                    var iLabels = issue.TryGetProperty("labels", out var il)
                        ? string.Join(", ", il.EnumerateArray()
                            .Select(l => l.TryGetProperty("name", out var ln) ? ln.GetString() : null)
                            .Where(l => l is not null))
                        : null;

                    sb.AppendLine($"## Issue #{issueNumber}: {iTitle}");
                    if (!string.IsNullOrEmpty(iState))   sb.AppendLine($"**State:** {iState}");
                    if (!string.IsNullOrEmpty(iLabels))  sb.AppendLine($"**Labels:** {iLabels}");
                    if (!string.IsNullOrWhiteSpace(iBody))
                    {
                        sb.AppendLine();
                        // Truncate long bodies to keep context size manageable
                        var body = iBody.Length > 1000 ? iBody[..1000] + "\n…(truncated)" : iBody;
                        sb.AppendLine(body);
                    }
                    hasContent = true;
                }
                catch (JsonException) { /* skip */ }
            }
        }

        // Open PR for the current branch
        var prJson = Gh("pr view --json number,title,state,url,isDraft,reviewDecision", workingDirectory);
        if (prJson is not null)
        {
            try
            {
                var pr     = JsonDocument.Parse(prJson).RootElement;
                var number = pr.TryGetProperty("number",         out var n) ? n.GetInt32().ToString() : null;
                var title  = pr.TryGetProperty("title",          out var t) ? t.GetString()           : null;
                var state  = pr.TryGetProperty("state",          out var s) ? s.GetString()           : null;
                var url    = pr.TryGetProperty("url",            out var u) ? u.GetString()           : null;
                var draft  = pr.TryGetProperty("isDraft",        out var d) && d.GetBoolean();
                var review = pr.TryGetProperty("reviewDecision", out var r) ? r.GetString()           : null;

                if (hasContent) sb.AppendLine();
                sb.AppendLine($"## PR #{number}: {title}");
                var stateLabel = draft ? "Draft" : state ?? "unknown";
                sb.AppendLine($"**State:** {stateLabel}{(url is not null ? $" — {url}" : "")}");
                if (!string.IsNullOrEmpty(review) && review != "REVIEW_REQUIRED")
                    sb.AppendLine($"**Review:** {review}");

                // Review comments
                var reviewsJson = Gh("pr view --json reviews", workingDirectory);
                if (reviewsJson is not null)
                {
                    try
                    {
                        var reviews = JsonDocument.Parse(reviewsJson).RootElement
                            .GetProperty("reviews");
                        var nonEmpty = reviews.EnumerateArray()
                            .Where(rv => rv.TryGetProperty("body", out var b) && !string.IsNullOrWhiteSpace(b.GetString()))
                            .ToList();
                        if (nonEmpty.Count > 0)
                        {
                            sb.AppendLine("\n**Review comments:**");
                            foreach (var rv in nonEmpty)
                            {
                                var author = rv.TryGetProperty("author", out var a)
                                    && a.TryGetProperty("login", out var l) ? l.GetString() : "?";
                                var rvState = rv.TryGetProperty("state", out var rs) ? rs.GetString() : null;
                                var body    = rv.TryGetProperty("body",  out var rb) ? rb.GetString() : "";
                                if (body?.Length > 300) body = body[..300] + "…";
                                sb.AppendLine($"- **{author}** ({rvState}): {body}");
                            }
                        }
                    }
                    catch (JsonException) { /* skip */ }
                }

                // CI checks
                var checksOutput = Gh("pr checks --json name,state,conclusion", workingDirectory);
                if (checksOutput is not null)
                {
                    try
                    {
                        var checks = JsonDocument.Parse(checksOutput).RootElement;
                        if (checks.ValueKind == JsonValueKind.Array && checks.GetArrayLength() > 0)
                        {
                            sb.AppendLine("\n**CI Checks:**");
                            foreach (var check in checks.EnumerateArray())
                            {
                                var checkName       = check.TryGetProperty("name",       out var cn) ? cn.GetString() : "?";
                                var checkConclusion = check.TryGetProperty("conclusion", out var cc) ? cc.GetString() : null;
                                var checkState      = check.TryGetProperty("state",      out var cs) ? cs.GetString() : null;
                                var status = checkConclusion ?? checkState ?? "pending";
                                var icon   = status.ToUpperInvariant() switch
                                {
                                    "SUCCESS" => "✓",
                                    "FAILURE" or "ERROR" => "✗",
                                    _ => "○"
                                };
                                sb.AppendLine($"- {icon} `{checkName}` — {status}");
                            }
                        }
                    }
                    catch (JsonException) { /* skip */ }
                }
                hasContent = true;
            }
            catch (JsonException) { /* skip */ }
        }

        return hasContent ? sb.ToString().TrimEnd() : string.Empty;
    }

    private static bool IsGhAvailable(string workingDirectory) =>
        Gh("--version", workingDirectory) is not null;

    private static string? Gh(string args, string workingDirectory)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("gh", args)
            {
                WorkingDirectory       = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
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

    /// <summary>
    /// Builds a directory tree of all git-tracked and untracked files.
    /// Untracked files are marked with (*).
    /// </summary>
    private static string BuildFileExplorerContext(string workingDirectory)
    {
        var tracked   = Git("ls-files", workingDirectory)?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var untracked = Git("ls-files --others --exclude-standard", workingDirectory)?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? [];

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

    private static string? Git(string args, string workingDirectory)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("git", args)
            {
                WorkingDirectory       = workingDirectory,
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
        string.Join("\n\n---\n\n",
            GetSystemPrompt(sessionPath)
                .Concat(GetContextMessages(sessionPath))
                .Where(s => !string.IsNullOrWhiteSpace(s)));

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigits();
}
