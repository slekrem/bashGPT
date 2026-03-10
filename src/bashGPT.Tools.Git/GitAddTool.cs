using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Git;

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
            (repoPath, files) = ParseInput(call.ArgumentsJson);
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

    private static (string Path, string Files) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var files = root.GetProperty("files").GetString() ?? throw new ArgumentException("files must not be null");
        var path = root.TryGetProperty("path", out var p) ? p.GetString() ?? Directory.GetCurrentDirectory() : Directory.GetCurrentDirectory();
        return (path, files);
    }
}
