using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub.PullRequests;

public sealed class GhPrReviewTool : ITool
{
    private readonly IGhPolicy _policy;

    public GhPrReviewTool(IGhPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGhPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_review",
        Description: "Submits a review on a GitHub pull request: approve, request changes, or leave a comment. If no number is given, uses the PR for the current branch. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("event",  "string",  "Review type: 'approve', 'request-changes', or 'comment'.",                                Required: true),
            new ToolParameter("body",   "string",  "Review comment body (markdown supported). Required for 'request-changes' and 'comment'.", Required: false),
            new ToolParameter("number", "integer", "PR number. If omitted, uses the open PR for the current branch.",                         Required: false),
            new ToolParameter("path",   "string",  "Path to the git repository. Defaults to current directory.",                              Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, reviewEvent, body;
        int? number;
        try
        {
            (repoPath, reviewEvent, body, number) = ParseArgs(call.ArgumentsJson, call.WorkingDirectory);
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

        var args = new List<string> { "pr", "review" };
        if (number.HasValue) args.Add(number.Value.ToString());

        args.Add(reviewEvent switch
        {
            "approve"         => "--approve",
            "request-changes" => "--request-changes",
            _                 => "--comment",
        });

        if (!string.IsNullOrWhiteSpace(body)) { args.Add("--body"); args.Add(body); }

        var (stdout, stderr, exit) = await GhRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"gh pr review failed: {stderr.Trim()}");

        var label = number.HasValue ? $"PR #{number}" : "PR";
        return new ToolResult(Success: true, Content: $"Review submitted on {label} ({reviewEvent}).");
    }

    private static (string repoPath, string reviewEvent, string body, int? number) ParseArgs(string json, string? workingDirectory)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("event", out var eventEl) || eventEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(eventEl.GetString()))
            throw new ArgumentException("missing_required_field: 'event' is required. Use 'approve', 'request-changes', or 'comment'.");

        var reviewEvent = eventEl.GetString()!;
        if (reviewEvent is not ("approve" or "request-changes" or "comment"))
            throw new ArgumentException("invalid_value: 'event' must be 'approve', 'request-changes', or 'comment'.");

        if (reviewEvent is "request-changes" or "comment"
            && (!root.TryGetProperty("body", out var bodyCheck) || bodyCheck.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(bodyCheck.GetString())))
            throw new ArgumentException($"missing_required_field: 'body' is required when event is '{reviewEvent}'.");

        var body = root.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind == JsonValueKind.String
            ? bodyEl.GetString() ?? ""
            : "";

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

        return (repoPath, reviewEvent, body, number);
    }
}
