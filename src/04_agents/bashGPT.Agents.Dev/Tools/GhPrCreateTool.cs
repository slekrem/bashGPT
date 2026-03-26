using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: creates a GitHub pull request for the current branch via gh CLI.
/// </summary>
public sealed class GhPrCreateTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_create",
        Description: "Creates a GitHub pull request for the current branch. Requires gh CLI to be installed and authenticated.",
        Parameters:
        [
            new ToolParameter("title", "string",  "PR title.",                                                                Required: true),
            new ToolParameter("body",  "string",  "PR description (markdown supported).",                                     Required: false),
            new ToolParameter("base",  "string",  "Base branch to merge into. Defaults to the repository default branch.",    Required: false),
            new ToolParameter("draft", "boolean", "Create as draft PR. Defaults to false.",                                   Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string title, body, base_;
        bool draft;
        try
        {
            (title, body, base_, draft) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var args = new List<string> { "pr", "create", "--title", title };
        if (!string.IsNullOrWhiteSpace(body))  { args.Add("--body");  args.Add(body); }
        else                                     { args.Add("--body");  args.Add(""); }
        if (!string.IsNullOrWhiteSpace(base_)) { args.Add("--base");  args.Add(base_); }
        if (draft)                               { args.Add("--draft"); }

        var (output, error, exitCode) = RunGh(args, workingDirectory);

        if (exitCode != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"gh pr create failed: {error ?? output ?? "unknown error"}"));

        return Task.FromResult(new ToolResult(Success: true, Content: $"Pull request created: {output?.Trim()}"));
    }

    private static (string output, string error, int exitCode) RunGh(IEnumerable<string> args, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo("gh")
            {
                WorkingDirectory      = workingDirectory,
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

    private static (string title, string body, string base_, bool draft) ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("title", out var titleEl) || titleEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(titleEl.GetString()))
            throw new ArgumentException("missing_required_field: 'title' is required and must be a non-empty string.");

        var body  = root.TryGetProperty("body",  out var bodyEl)  && bodyEl.ValueKind  == JsonValueKind.String ? bodyEl.GetString()  ?? "" : "";
        var base_ = root.TryGetProperty("base",  out var baseEl)  && baseEl.ValueKind  == JsonValueKind.String ? baseEl.GetString()  ?? "" : "";
        var draft = root.TryGetProperty("draft", out var draftEl) && draftEl.ValueKind == JsonValueKind.True;

        return (titleEl.GetString()!, body, base_, draft);
    }
}
