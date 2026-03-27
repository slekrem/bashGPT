using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.PullRequests;

public sealed class GhPrMergeTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhPrMergeTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_merge",
        Description: "Merges a GitHub pull request. If no number is given, uses the PR for the current branch. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("number", "integer", "PR number. If omitted, uses the open PR for the current branch.",        Required: false),
            new ToolParameter("method", "string",  "Merge method: 'merge', 'squash', or 'rebase'. Defaults to 'merge'.",     Required: false),
            new ToolParameter("path",   "string",  "Path to the git repository. Defaults to current directory.",             Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, method;
        int? number;
        try
        {
            (repoPath, number, method) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }

        if (!_policy.AllowWrite(repoPath))
            return new ToolResult(Success: false, Content: "Write blocked by policy.");

        var args = new List<string> { "pr", "merge" };
        if (number.HasValue) args.Add(number.Value.ToString());

        args.Add(method switch
        {
            "squash" => "--squash",
            "rebase" => "--rebase",
            _        => "--merge",
        });

        // Avoid interactive prompts
        args.Add("--auto");

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh pr merge failed: {stderr.Trim()}");

        var label = number.HasValue ? $"PR #{number}" : "PR";
        return new ToolResult(Success: true, Content: $"{label} merged ({method}).");
    }

    private static (string repoPath, int? number, string method) ParseArgs(string json, string? workingDirectory)
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

        var method = "merge";
        if (root.TryGetProperty("method", out var methodEl))
        {
            if (methodEl.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new ArgumentException("invalid_type: 'method' must be a string.");
            var m = methodEl.ValueKind == JsonValueKind.String ? methodEl.GetString() : null;
            if (!string.IsNullOrWhiteSpace(m))
            {
                if (m is not ("merge" or "squash" or "rebase"))
                    throw new ArgumentException("invalid_value: 'method' must be 'merge', 'squash', or 'rebase'.");
                method = m;
            }
        }

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, number, method);
    }
}
