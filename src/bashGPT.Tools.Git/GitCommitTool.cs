using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Git;

public sealed class GitCommitTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitCommitTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_commit",
        Description: "Creates a commit with the staged changes. Blocked by default policy.",
        Parameters:
        [
            new ToolParameter("message", "string", "Commit message.", Required: true),
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, message;
        try
        {
            (repoPath, message) = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowWrite(repoPath))
            return new ToolResult(Success: false, Content: "Write blocked by policy.");

        // Sanitize message to prevent argument injection
        var sanitized = message.Replace("\"", "\\\"");
        var (stdout, stderr, exit) = await GitRunner.RunAsync($"commit -m \"{sanitized}\"", repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git commit failed: {stderr.Trim()}");

        var result = new { message, output = stdout.Trim() };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, string Message) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var message = root.GetProperty("message").GetString() ?? throw new ArgumentException("message must not be null");
        var path = root.TryGetProperty("path", out var p) ? p.GetString() ?? Directory.GetCurrentDirectory() : Directory.GetCurrentDirectory();
        return (path, message);
    }
}
