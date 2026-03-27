using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.PullRequests;

public sealed class GhPrListTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhPrListTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_list",
        Description: "Lists GitHub pull requests for the repository. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("state", "string",  "Filter by state: 'open', 'closed', 'merged', or 'all'. Defaults to 'open'.", Required: false),
            new ToolParameter("limit", "integer", "Maximum number of pull requests to return. Defaults to 30.",                  Required: false),
            new ToolParameter("path",  "string",  "Path to the git repository. Defaults to current directory.",                  Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, state;
        int limit;
        try
        {
            (repoPath, state, limit) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { "pr", "list", "--state", state, "--limit", limit.ToString(), "--json", "number,title,state,author,isDraft,headRefName,createdAt" };
        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh pr list failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: stdout.Trim());
    }

    private static (string repoPath, string state, int limit) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        var state = "open";
        if (root.TryGetProperty("state", out var stateEl))
        {
            if (stateEl.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new ArgumentException("invalid_type: 'state' must be a string.");
            var s = stateEl.ValueKind == JsonValueKind.String ? stateEl.GetString() : null;
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (s is not ("open" or "closed" or "merged" or "all"))
                    throw new ArgumentException("invalid_value: 'state' must be 'open', 'closed', 'merged', or 'all'.");
                state = s;
            }
        }

        var limit = 30;
        if (root.TryGetProperty("limit", out var limitEl))
        {
            if (limitEl.ValueKind is not JsonValueKind.Number)
                throw new ArgumentException("invalid_type: 'limit' must be an integer.");
            if (!limitEl.TryGetInt32(out var l) || l <= 0)
                throw new ArgumentException("invalid_value: 'limit' must be a positive integer.");
            limit = l;
        }

        return (repoPath, state, limit);
    }
}
