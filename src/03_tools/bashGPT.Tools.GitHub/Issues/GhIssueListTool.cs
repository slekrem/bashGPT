using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.Issues;

public sealed class GhIssueListTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhIssueListTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_issue_list",
        Description: "Lists GitHub issues for the repository. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("state", "string",  "Filter by state: 'open', 'closed', or 'all'. Defaults to 'open'.", Required: false),
            new ToolParameter("label", "string",  "Filter by label name.",                                             Required: false),
            new ToolParameter("limit", "integer", "Maximum number of issues to return. Defaults to 30.",              Required: false),
            new ToolParameter("path",  "string",  "Path to the git repository. Defaults to current directory.",       Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        string state;
        string? label;
        int limit;
        try
        {
            (repoPath, state, label, limit) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { "issue", "list", "--state", state, "--limit", limit.ToString(), "--json", "number,title,state,labels,assignees,createdAt" };
        if (!string.IsNullOrWhiteSpace(label)) { args.Add("--label"); args.Add(label); }

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh issue list failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: stdout.Trim());
    }

    private static (string repoPath, string state, string? label, int limit) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl))
        {
            if (pathEl.ValueKind is JsonValueKind.Null) { /* use cwd */ }
            else if (pathEl.ValueKind is not JsonValueKind.String)
                throw new ArgumentException("invalid_type: 'path' must be a string.");
            else
            {
                var p = pathEl.GetString();
                if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
            }
        }

        var state = "open";
        if (root.TryGetProperty("state", out var stateEl))
        {
            if (stateEl.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new ArgumentException("invalid_type: 'state' must be a string.");
            var s = stateEl.ValueKind == JsonValueKind.String ? stateEl.GetString() : null;
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (s is not ("open" or "closed" or "all"))
                    throw new ArgumentException("invalid_value: 'state' must be 'open', 'closed', or 'all'.");
                state = s;
            }
        }

        string? label = null;
        if (root.TryGetProperty("label", out var labelEl) && labelEl.ValueKind == JsonValueKind.String)
            label = labelEl.GetString();

        var limit = 30;
        if (root.TryGetProperty("limit", out var limitEl))
        {
            if (limitEl.ValueKind is not JsonValueKind.Number)
                throw new ArgumentException("invalid_type: 'limit' must be an integer.");
            if (!limitEl.TryGetInt32(out var l) || l <= 0)
                throw new ArgumentException("invalid_value: 'limit' must be a positive integer.");
            limit = l;
        }

        return (repoPath, state, label, limit);
    }
}
