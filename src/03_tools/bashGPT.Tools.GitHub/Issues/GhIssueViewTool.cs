using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.Issues;

public sealed class GhIssueViewTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhIssueViewTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_issue_view",
        Description: "Returns details of a GitHub issue including title, body, labels, and comments. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("number", "integer", "Issue number.",                                                    Required: true),
            new ToolParameter("path",   "string",  "Path to the git repository. Defaults to current directory.",      Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        int number;
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

        var args = new List<string> { "issue", "view", number.ToString(), "--json", "number,title,state,body,labels,assignees,comments,createdAt,url" };
        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh issue view failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: stdout.Trim());
    }

    private static (string repoPath, int number) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("number", out var numEl) || numEl.ValueKind != JsonValueKind.Number
            || !numEl.TryGetInt32(out var number) || number <= 0)
            throw new ArgumentException("missing_required_field: 'number' is required and must be a positive integer.");

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, number);
    }
}
