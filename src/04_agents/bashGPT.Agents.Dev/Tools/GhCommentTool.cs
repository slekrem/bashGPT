using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: adds a comment to a GitHub issue or pull request via gh CLI.
/// </summary>
public sealed class GhCommentTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "gh_comment",
        Description: "Adds a comment to a GitHub issue or pull request. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("number", "integer", "Issue or PR number to comment on.",   Required: true),
            new ToolParameter("body",   "string",  "Comment text (markdown supported).",  Required: true),
            new ToolParameter("type",   "string",  "Either 'issue' or 'pr'. Defaults to 'issue'.", Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        int number;
        string body, type;
        try
        {
            (number, body, type) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var (output, error, exitCode) = RunGh(
            [type, "comment", number.ToString(), "--body", body],
            workingDirectory);

        if (exitCode != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"gh {type} comment failed: {error ?? output ?? "unknown error"}"));

        return Task.FromResult(new ToolResult(Success: true, Content: $"Comment added to {type} #{number}."));
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

    private static (int number, string body, string type) ParseArgs(string json)
    {
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

        return (number, bodyEl.GetString()!, type);
    }
}
