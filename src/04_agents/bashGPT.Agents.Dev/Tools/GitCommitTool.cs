using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: stages files and creates a git commit.
/// </summary>
public sealed class GitCommitTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "git_commit",
        Description: "Stages files and creates a git commit. If no paths are given, all changed tracked files are staged (git add -A).",
        Parameters:
        [
            new ToolParameter("message", "string", "Commit message. Use conventional commit format: feat:, fix:, refactor:, test:, docs:, chore:", Required: true),
            new ToolParameter("paths",   "array",  "File paths to stage. If omitted or empty, all changes are staged.",                           Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string message;
        string[] paths;
        try
        {
            (message, paths) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        // Stage
        var addArgs = paths.Length > 0
            ? (IEnumerable<string>)["add", .. paths]
            : ["add", "-A"];

        var (_, addError, addExit) = RunGit(addArgs, workingDirectory);
        if (addExit != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"git add failed: {addError}"));

        // Commit
        var (commitOut, commitError, commitExit) = RunGit(["commit", "-m", message], workingDirectory);
        if (commitExit != 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"git commit failed: {commitError ?? commitOut}"));

        return Task.FromResult(new ToolResult(Success: true, Content: commitOut.Trim()));
    }

    private static (string output, string error, int exitCode) RunGit(IEnumerable<string> args, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo("git")
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

    private static (string message, string[] paths) ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(msgEl.GetString()))
            throw new ArgumentException("missing_required_field: 'message' is required and must be a non-empty string.");

        var paths = Array.Empty<string>();
        if (root.TryGetProperty("paths", out var pathsEl) && pathsEl.ValueKind == JsonValueKind.Array)
        {
            paths = pathsEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(e.GetString()))
                .Select(e => e.GetString()!)
                .ToArray();
        }

        return (msgEl.GetString()!, paths);
    }
}
