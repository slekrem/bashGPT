using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: submits a GitHub PR review via gh CLI.
/// </summary>
public sealed class GhPrReviewTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_review",
        Description: "Submits a review on a GitHub pull request: approve, request changes, or leave a comment. If no number is given, uses the PR for the current branch.",
        Parameters:
        [
            new ToolParameter("event",  "string",  "Review type: 'approve', 'request-changes', or 'comment'.",                                Required: true),
            new ToolParameter("body",   "string",  "Review comment body (markdown supported). Required for 'request-changes' and 'comment'.", Required: false),
            new ToolParameter("number", "integer", "PR number. If omitted, uses the open PR for the current branch.",                         Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string reviewEvent, body;
        int? number;
        try
        {
            (reviewEvent, body, number) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var args = new List<string> { "pr", "review" };
        if (number.HasValue) args.Add(number.Value.ToString());

        args.Add(reviewEvent switch
        {
            "approve"          => "--approve",
            "request-changes"  => "--request-changes",
            _                  => "--comment",
        });

        if (!string.IsNullOrWhiteSpace(body)) { args.Add("--body"); args.Add(body); }

        var (output, error, exitCode) = RunGh(args, workingDirectory);

        if (exitCode != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"gh pr review failed: {error ?? output ?? "unknown error"}"));

        var label = number.HasValue ? $"PR #{number}" : "PR";
        return Task.FromResult(new ToolResult(Success: true, Content: $"Review submitted on {label} ({reviewEvent})."));
    }

    private static (string output, string error, int exitCode) RunGh(IEnumerable<string> args, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo("gh")
            {
                WorkingDirectory       = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            var error  = proc.StandardError.ReadToEnd().Trim();
            proc.WaitForExit();
            return (output, error, proc.ExitCode);
        }
        catch (Exception ex)
        {
            return (string.Empty, ex.Message, -1);
        }
    }

    private static (string reviewEvent, string body, int? number) ParseArgs(string json)
    {
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

        return (reviewEvent, body, number);
    }
}
