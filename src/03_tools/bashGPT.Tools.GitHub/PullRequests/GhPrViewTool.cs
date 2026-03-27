using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.PullRequests;

public sealed class GhPrViewTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhPrViewTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_view",
        Description: "Returns details of a GitHub pull request including title, body, state, review decision, and CI checks. If no number is given, uses the PR for the current branch. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("number", "integer", "PR number. If omitted, uses the open PR for the current branch.", Required: false),
            new ToolParameter("path",   "string",  "Path to the git repository. Defaults to current directory.",      Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        int? number;
        try
        {
            (repoPath, number) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }

        if (!_policy.AllowRead(repoPath))
            return new ToolResult(Success: false, Content: "Read blocked by policy.");

        var args = new List<string> { "pr", "view" };
        if (number.HasValue) args.Add(number.Value.ToString());
        args.AddRange(["--json", "number,title,state,url,isDraft,reviewDecision,body,author,headRefName,baseRefName,createdAt"]);

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh pr view failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: stdout.Trim());
    }

    private static (string repoPath, int? number) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int? number = null;
        if (root.TryGetProperty("number", out var numEl) && numEl.ValueKind == JsonValueKind.Number)
        {
            if (!numEl.TryGetInt32(out var n) || n <= 0)
                throw new ArgumentException("invalid_value: 'number' must be a positive integer.");
            number = n;
        }

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, number);
    }
}
