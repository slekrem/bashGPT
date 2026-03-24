using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

public sealed class GitLogTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitLogTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_log",
        Description: "Returns the commit history as structured entries (hash, author, date, message).",
        Parameters:
        [
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
            new ToolParameter("limit", "integer", "Maximum number of commits to return. Default: 20.", Required: false),
            new ToolParameter("branch", "string", "Branch or ref to log. Default: current branch.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, branch;
        int limit;
        try
        {
            (repoPath, limit, branch) = ParseInput(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowRead(repoPath))
            return new ToolResult(Success: false, Content: "Read blocked by policy.");

        // Format: hash|author|date|subject
        var args = $"log --pretty=format:\"%H|%an|%ai|%s\" -n {limit}";
        if (!string.IsNullOrWhiteSpace(branch))
            args += $" {branch}";

        var (stdout, stderr, exit) = await GitRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git log failed: {stderr.Trim()}");

        var commits = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var parts = line.Split('|', 4);
                return parts.Length == 4
                    ? (object)new { hash = parts[0], author = parts[1], date = parts[2], message = parts[3] }
                    : (object)new { raw = line };
            })
            .ToList();

        var result = new { commits };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, int Limit, string Branch) ParseInput(string json, string? workingDirectory = null)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

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

        int limit = root.TryGetProperty("limit", out var l)
            ? l.ValueKind switch
            {
                JsonValueKind.Number when l.TryGetInt32(out var i) => i,
                JsonValueKind.Null => 20,
                _ => throw new ArgumentException("invalid_type: 'limit' must be an integer."),
            }
            : 20;
        if (limit <= 0)
            throw new ArgumentException("invalid_value: 'limit' must be greater than 0.");

        var branch = root.TryGetProperty("branch", out var b)
            ? b.ValueKind switch
            {
                JsonValueKind.String => b.GetString() ?? string.Empty,
                JsonValueKind.Null => string.Empty,
                _ => throw new ArgumentException("invalid_type: 'branch' must be a string."),
            }
            : string.Empty;

        return (path, limit, branch);
    }
}
