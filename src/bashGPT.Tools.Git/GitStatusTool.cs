using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Git;

public sealed class GitStatusTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitStatusTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_status",
        Description: "Returns the working tree status (staged, unstaged, untracked files).",
        Parameters:
        [
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        try
        {
            repoPath = ParsePath(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }

        if (!_policy.AllowRead(repoPath))
            return new ToolResult(Success: false, Content: "Read blocked by policy.");

        var (stdout, stderr, exit) = await GitRunner.RunAsync("status --porcelain=v1 -b", repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git status failed: {stderr.Trim()}");

        var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var branch = string.Empty;
        var staged = new List<string>();
        var unstaged = new List<string>();
        var untracked = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                branch = line[3..].Split("...")[0];
                continue;
            }
            if (line.Length < 2) continue;

            var x = line[0];
            var y = line[1];
            var file = line[3..];

            if (x != ' ' && x != '?') staged.Add(file);
            if (y == 'M' || y == 'D') unstaged.Add(file);
            if (x == '?' && y == '?') untracked.Add(file);
        }

        var result = new { branch, staged, unstaged, untracked };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static string ParsePath(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("path", out var p))
            return Directory.GetCurrentDirectory();
        if (p.ValueKind is JsonValueKind.Null)
            return Directory.GetCurrentDirectory();
        if (p.ValueKind is not JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'path' must be a string.");

        var path = p.GetString();
        return string.IsNullOrWhiteSpace(path) ? Directory.GetCurrentDirectory() : path;
    }
}
