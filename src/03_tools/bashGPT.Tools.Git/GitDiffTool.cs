using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

public sealed class GitDiffTool : ITool
{
    private const int MaxChars = 65_536;

    private readonly IGitPolicy _policy;

    public GitDiffTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_diff",
        Description: "Returns the diff of staged or unstaged changes. Optionally scoped to specific files.",
        Parameters:
        [
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
            new ToolParameter("staged", "boolean", "Show staged (--cached) diff. Default: false (unstaged).", Required: false),
            new ToolParameter("files", "string", "Space-separated list of files to diff.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath;
        bool staged;
        string files;
        try
        {
            (repoPath, staged, files) = ParseInput(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowRead(repoPath))
            return new ToolResult(Success: false, Content: "Read blocked by policy.");

        var args = staged ? "diff --cached" : "diff";
        if (!string.IsNullOrWhiteSpace(files))
            args += $" -- {files}";

        var (stdout, stderr, exit) = await GitRunner.RunAsync(args, repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git diff failed: {stderr.Trim()}");

        var diff = stdout.Length > MaxChars ? stdout[..MaxChars] + "\n[truncated]" : stdout;
        var result = new { staged, diff };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, bool Staged, string Files) ParseInput(string json, string? workingDirectory = null)
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

        bool staged = root.TryGetProperty("staged", out var s)
            ? s.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => false,
                _ => throw new ArgumentException("invalid_type: 'staged' must be a boolean."),
            }
            : false;

        var files = root.TryGetProperty("files", out var f)
            ? f.ValueKind switch
            {
                JsonValueKind.String => f.GetString() ?? string.Empty,
                JsonValueKind.Null => string.Empty,
                _ => throw new ArgumentException("invalid_type: 'files' must be a string."),
            }
            : string.Empty;

        return (path, staged, files);
    }
}
