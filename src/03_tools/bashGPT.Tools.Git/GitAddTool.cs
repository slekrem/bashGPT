using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

public sealed class GitAddTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitAddTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_add",
        Description: "Stages files for commit. Blocked by default policy.",
        Parameters:
        [
            new ToolParameter("files", "string", "Space-separated list of files to stage. Use '.' for all.", Required: true),
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, files;
        try
        {
            (repoPath, files) = ParseInput(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowWrite(repoPath))
            return new ToolResult(Success: false, Content: "Write blocked by policy.");

        var (_, stderr, exit) = await GitRunner.RunAsync($"add {files}", repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git add failed: {stderr.Trim()}");

        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(new { staged = files }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, string Files) ParseInput(string json, string? workingDirectory = null)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("files", out var filesEl))
            throw new ArgumentException("missing_required_field: 'files' is required. Example: {\"files\":\".\",\"path\":\".\"}");
        if (filesEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'files' must be a string.");
        var files = filesEl.GetString();
        if (string.IsNullOrWhiteSpace(files))
            throw new ArgumentException("invalid_value: 'files' must not be empty.");

        var path = root.TryGetProperty("path", out var p)
            ? p.ValueKind switch
            {
                JsonValueKind.String => p.GetString() ?? cwd,
                JsonValueKind.Null => cwd,
                _ => throw new ArgumentException("invalid_type: 'path' must be a string."),
            }
            : cwd;
        if (string.IsNullOrWhiteSpace(path))
            path = cwd;

        return (path, files);
    }
}
