using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

public sealed class GitBranchTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitBranchTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_branch",
        Description: "Lists local and optionally remote branches.",
        Parameters:
        [
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
            new ToolParameter("remotes", "boolean", "Include remote branches. Default: false.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        bool remotes;
        try
        {
            (repoPath, remotes) = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowRead(repoPath))
            return new ToolResult(Success: false, Content: "Read blocked by policy.");

        var args = remotes ? "branch -a" : "branch";
        var (stdout, stderr, exit) = await GitRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git branch failed: {stderr.Trim()}");

        var branches = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => new { name = l.TrimStart('*').Trim(), current = l.StartsWith('*') })
            .ToList();

        var result = new { branches };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, bool Remotes) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var path = root.TryGetProperty("path", out var p)
            ? p.ValueKind switch
            {
                JsonValueKind.String => p.GetString() ?? Directory.GetCurrentDirectory(),
                JsonValueKind.Null => Directory.GetCurrentDirectory(),
                _ => throw new ArgumentException("invalid_type: 'path' must be a string."),
            }
            : Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(path))
            path = Directory.GetCurrentDirectory();

        bool remotes = root.TryGetProperty("remotes", out var r)
            ? r.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => false,
                _ => throw new ArgumentException("invalid_type: 'remotes' must be a boolean."),
            }
            : false;

        return (path, remotes);
    }
}
