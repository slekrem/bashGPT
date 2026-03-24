using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

public sealed class GitCheckoutTool : ITool
{
    private readonly IGitPolicy _policy;

    public GitCheckoutTool(IGitPolicy? policy = null)
    {
        _policy = policy ?? new DefaultGitPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "git_checkout",
        Description: "Switches to a branch or creates a new one. Blocked by default policy.",
        Parameters:
        [
            new ToolParameter("branch", "string", "Branch name to switch to.", Required: true),
            new ToolParameter("create", "boolean", "Create a new branch (-b). Default: false.", Required: false),
            new ToolParameter("path", "string", "Path to the git repository. Default: current directory.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string repoPath, branch;
        bool create;
        try
        {
            (repoPath, branch, create) = ParseInput(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.AllowWrite(repoPath))
            return new ToolResult(Success: false, Content: "Write blocked by policy.");

        var flag = create ? "-b " : string.Empty;
        var (stdout, stderr, exit) = await GitRunner.RunAsync($"checkout {flag}{branch}", repoPath, ct);

        if (exit != 0)
            return new ToolResult(Success: false, Content: $"git checkout failed: {stderr.Trim()}");

        var result = new { branch, created = create, output = (stdout + stderr).Trim() };
        return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (string Path, string Branch, bool Create) ParseInput(string json, string? workingDirectory = null)
    {
        var cwd = workingDirectory ?? Directory.GetCurrentDirectory();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("branch", out var branchEl))
            throw new ArgumentException("missing_required_field: 'branch' is required.");
        if (branchEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'branch' must be a string.");
        var branch = branchEl.GetString();
        if (string.IsNullOrWhiteSpace(branch))
            throw new ArgumentException("invalid_value: 'branch' must not be empty.");

        bool create = root.TryGetProperty("create", out var c)
            ? c.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => false,
                _ => throw new ArgumentException("invalid_type: 'create' must be a boolean."),
            }
            : false;

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

        return (path, branch, create);
    }
}
