using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: returns the diff of a GitHub pull request via gh CLI.
/// </summary>
public sealed class GhPrDiffTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "gh_pr_diff",
        Description: "Returns the diff of a GitHub pull request. If no number is given, uses the PR for the current branch.",
        Parameters:
        [
            new ToolParameter("number", "integer", "PR number. If omitted, uses the open PR for the current branch.", Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        int? number;
        try
        {
            number = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var args = number.HasValue
            ? (IEnumerable<string>)["pr", "diff", number.Value.ToString()]
            : ["pr", "diff"];

        var (output, error, exitCode) = RunGh(args, workingDirectory);

        if (exitCode != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"gh pr diff failed: {error ?? output ?? "unknown error"}"));

        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult(new ToolResult(Success: true, Content: "No diff — the PR has no changes."));

        return Task.FromResult(new ToolResult(Success: true, Content: output));
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

    private static int? ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("number", out var numEl) && numEl.ValueKind == JsonValueKind.Number)
        {
            if (!numEl.TryGetInt32(out var n) || n <= 0)
                throw new ArgumentException("invalid_value: 'number' must be a positive integer.");
            return n;
        }

        return null;
    }
}
