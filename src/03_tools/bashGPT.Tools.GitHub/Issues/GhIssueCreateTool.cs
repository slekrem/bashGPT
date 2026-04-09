using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.Issues;

public sealed class GhIssueCreateTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhIssueCreateTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_issue_create",
        Description: "Creates a new GitHub issue. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("title",    "string", "Issue title.",                                                   Required: true),
            new ToolParameter("body",     "string", "Issue description (markdown supported).",                        Required: false),
            new ToolParameter("label",    "string", "Label to assign (can be specified once).",                       Required: false),
            new ToolParameter("assignee", "string", "GitHub username to assign the issue to.",                        Required: false),
            new ToolParameter("path",     "string", "Path to the git repository. Defaults to current directory.",     Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, title, body, label, assignee;
        try
        {
            (repoPath, title, body, label, assignee) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { "issue", "create", "--title", title };
        if (!string.IsNullOrWhiteSpace(body))     { args.Add("--body");     args.Add(body); }
        else                                        { args.Add("--body");     args.Add(""); }
        if (!string.IsNullOrWhiteSpace(label))    { args.Add("--label");    args.Add(label); }
        if (!string.IsNullOrWhiteSpace(assignee)) { args.Add("--assignee"); args.Add(assignee); }

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh issue create failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: $"Issue created: {stdout.Trim()}");
    }

    private static (string repoPath, string title, string body, string label, string assignee) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("title", out var titleEl) || titleEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(titleEl.GetString()))
            throw new ArgumentException("missing_required_field: 'title' is required and must be a non-empty string.");

        var body     = root.TryGetProperty("body",     out var bodyEl)     && bodyEl.ValueKind     == JsonValueKind.String ? bodyEl.GetString()     ?? "" : "";
        var label    = root.TryGetProperty("label",    out var labelEl)    && labelEl.ValueKind    == JsonValueKind.String ? labelEl.GetString()    ?? "" : "";
        var assignee = root.TryGetProperty("assignee", out var assigneeEl) && assigneeEl.ValueKind == JsonValueKind.String ? assigneeEl.GetString() ?? "" : "";

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, titleEl.GetString()!, body, label, assignee);
    }
}
