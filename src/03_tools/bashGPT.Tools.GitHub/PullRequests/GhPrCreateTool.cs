using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.PullRequests;

public sealed class GhPrCreateTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhPrCreateTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_create",
        Description: "Creates a GitHub pull request for the current branch. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("title", "string",  "PR title.",                                                                Required: true),
            new ToolParameter("body",  "string",  "PR description (markdown supported).",                                     Required: false),
            new ToolParameter("base",  "string",  "Base branch to merge into. Defaults to the repository default branch.",    Required: false),
            new ToolParameter("draft", "boolean", "Create as draft PR. Defaults to false.",                                   Required: false),
            new ToolParameter("path",  "string",  "Path to the git repository. Defaults to current directory.",               Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, title, body, base_;
        bool draft;
        try
        {
            (repoPath, title, body, base_, draft) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { "pr", "create", "--title", title };
        if (!string.IsNullOrWhiteSpace(body)) { args.Add("--body"); args.Add(body); }
        else                                    { args.Add("--body"); args.Add(""); }
        if (!string.IsNullOrWhiteSpace(base_)) { args.Add("--base"); args.Add(base_); }
        if (draft) args.Add("--draft");

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh pr create failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: $"Pull request created: {stdout.Trim()}");
    }

    private static (string repoPath, string title, string body, string base_, bool draft) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("title", out var titleEl) || titleEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(titleEl.GetString()))
            throw new ArgumentException("missing_required_field: 'title' is required and must be a non-empty string.");

        var body  = root.TryGetProperty("body",  out var bodyEl)  && bodyEl.ValueKind  == JsonValueKind.String ? bodyEl.GetString()  ?? "" : "";
        var base_ = root.TryGetProperty("base",  out var baseEl)  && baseEl.ValueKind  == JsonValueKind.String ? baseEl.GetString()  ?? "" : "";
        var draft = root.TryGetProperty("draft", out var draftEl) && draftEl.ValueKind == JsonValueKind.True;

        var repoPath = cwd;
        if (root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
        {
            var p = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(p)) repoPath = p;
        }

        return (repoPath, titleEl.GetString()!, body, base_, draft);
    }
}
