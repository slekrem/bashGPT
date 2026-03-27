using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.Comments;

public sealed class GhCommentTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhCommentTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_comment",
        Description: "Adds a comment to a GitHub issue or pull request. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("number", "integer", "Issue or PR number to comment on.",                          Required: true),
            new ToolParameter("body",   "string",  "Comment text (markdown supported).",                         Required: true),
            new ToolParameter("type",   "string",  "Either 'issue' or 'pr'. Defaults to 'issue'.",               Required: false),
            new ToolParameter("path",   "string",  "Path to the git repository. Defaults to current directory.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, body, type;
        int number;
        try
        {
            (repoPath, number, body, type) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { type, "comment", number.ToString(), "--body", body };
        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh {type} comment failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: $"Comment added to {type} #{number}.");
    }

    private static (string repoPath, int number, string body, string type) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("number", out var numEl) || numEl.ValueKind != JsonValueKind.Number
            || !numEl.TryGetInt32(out var number) || number <= 0)
            throw new ArgumentException("missing_required_field: 'number' is required and must be a positive integer.");

        if (!root.TryGetProperty("body", out var bodyEl) || bodyEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(bodyEl.GetString()))
            throw new ArgumentException("missing_required_field: 'body' is required and must be a non-empty string.");

        var type = root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
            ? typeEl.GetString() ?? "issue"
            : "issue";

        if (type != "issue" && type != "pr")
            throw new ArgumentException("invalid_value: 'type' must be either 'issue' or 'pr'.");

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, number, bodyEl.GetString()!, type);
    }
}
