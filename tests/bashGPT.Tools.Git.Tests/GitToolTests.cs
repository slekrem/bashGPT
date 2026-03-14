using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Git;

namespace bashGPT.Tools.Git.Tests;

/// <summary>
/// Tests run against a real temporary git repository.
/// </summary>
public class GitToolTests : IDisposable
{
    private readonly string _repoDir;

    public GitToolTests()
    {
        _repoDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_repoDir);

        // Init repo and create an initial commit so HEAD exists
        Run("init");
        Run("config user.email test@test.com");
        Run("config user.name Test");
        File.WriteAllText(Path.Combine(_repoDir, "readme.txt"), "hello");
        Run("add .");
        Run("commit -m \"initial commit\"");
    }

    public void Dispose() => DeleteDirectoryRobust(_repoDir);

    private void Run(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = _repoDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.WaitForExit();
    }

    // ── GitStatusTool ──────────────────────────────────────────────

    [Fact]
    public async Task GitStatus_CleanRepo_ReturnsEmptyLists()
    {
        var tool = new GitStatusTool();
        var result = await tool.ExecuteAsync(Call("git_status", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(0, output.GetProperty("staged").GetArrayLength());
        Assert.Equal(0, output.GetProperty("unstaged").GetArrayLength());
        Assert.Equal(0, output.GetProperty("untracked").GetArrayLength());
    }

    [Fact]
    public async Task GitStatus_UntrackedFile_ShowsInUntracked()
    {
        File.WriteAllText(Path.Combine(_repoDir, "new.txt"), "content");

        var tool = new GitStatusTool();
        var result = await tool.ExecuteAsync(Call("git_status", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("untracked").GetArrayLength());
    }

    [Fact]
    public async Task GitStatus_DefaultPolicyRead_Succeeds()
    {
        var tool = new GitStatusTool(new DefaultGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_status", new { path = _repoDir }), CancellationToken.None);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GitStatus_InvalidPathType_ReturnsStructuredValidationError()
    {
        var tool = new GitStatusTool();
        var result = await tool.ExecuteAsync(new ToolCall("git_status", """{"path":123}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'path'", result.Content, StringComparison.Ordinal);
    }

    // ── GitDiffTool ───────────────────────────────────────────────

    [Fact]
    public async Task GitDiff_ModifiedFile_ReturnsDiff()
    {
        File.WriteAllText(Path.Combine(_repoDir, "readme.txt"), "changed");

        var tool = new GitDiffTool();
        var result = await tool.ExecuteAsync(Call("git_diff", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Contains("changed", output.GetProperty("diff").GetString());
    }

    [Fact]
    public async Task GitDiff_NoDiff_ReturnsEmptyDiff()
    {
        var tool = new GitDiffTool();
        var result = await tool.ExecuteAsync(Call("git_diff", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(string.Empty, output.GetProperty("diff").GetString());
    }

    [Fact]
    public async Task GitDiff_InvalidStagedType_ReturnsStructuredValidationError()
    {
        var tool = new GitDiffTool();
        var result = await tool.ExecuteAsync(
            new ToolCall("git_diff", $$"""{"path":{{JsonSerializer.Serialize(_repoDir)}},"staged":"yes"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'staged'", result.Content, StringComparison.Ordinal);
    }

    // ── GitLogTool ────────────────────────────────────────────────

    [Fact]
    public async Task GitLog_InitialCommit_ReturnsOneEntry()
    {
        var tool = new GitLogTool();
        var result = await tool.ExecuteAsync(Call("git_log", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("commits").GetArrayLength());
        var commit = output.GetProperty("commits")[0];
        Assert.Equal("initial commit", commit.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GitLog_LimitOne_ReturnsMaxOne()
    {
        File.WriteAllText(Path.Combine(_repoDir, "f2.txt"), "x");
        Run("add .");
        Run("commit -m \"second commit\"");

        var tool = new GitLogTool();
        var result = await tool.ExecuteAsync(Call("git_log", new { path = _repoDir, limit = 1 }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("commits").GetArrayLength());
    }

    [Fact]
    public async Task GitLog_NonPositiveLimit_ReturnsStructuredValidationError()
    {
        var tool = new GitLogTool();
        var result = await tool.ExecuteAsync(Call("git_log", new { path = _repoDir, limit = 0 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'limit'", result.Content, StringComparison.Ordinal);
    }

    // ── GitBranchTool ────────────────────────────────────────────

    [Fact]
    public async Task GitBranch_ListsBranches_ContainsCurrent()
    {
        var tool = new GitBranchTool();
        var result = await tool.ExecuteAsync(Call("git_branch", new { path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        var branches = output.GetProperty("branches").EnumerateArray().ToList();
        Assert.NotEmpty(branches);
        Assert.Contains(branches, b => b.GetProperty("current").GetBoolean());
    }

    [Fact]
    public async Task GitBranch_InvalidRemotesType_ReturnsStructuredValidationError()
    {
        var tool = new GitBranchTool();
        var result = await tool.ExecuteAsync(
            new ToolCall("git_branch", $$"""{"path":{{JsonSerializer.Serialize(_repoDir)}},"remotes":"all"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'remotes'", result.Content, StringComparison.Ordinal);
    }

    // ── Write-ops blocked by DefaultGitPolicy ────────────────────

    [Fact]
    public async Task GitAdd_DefaultPolicy_Blocked()
    {
        var tool = new GitAddTool(new DefaultGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_add", new { files = ".", path = _repoDir }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GitAdd_MissingFiles_ReturnsStructuredValidationError()
    {
        var tool = new GitAddTool(new PermissiveGitPolicy());
        var result = await tool.ExecuteAsync(new ToolCall("git_add", $$"""{"path":{{JsonSerializer.Serialize(_repoDir)}}}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'files'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GitCommit_DefaultPolicy_Blocked()
    {
        var tool = new GitCommitTool(new DefaultGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_commit", new { message = "test", path = _repoDir }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GitCommit_MissingMessage_ReturnsStructuredValidationError()
    {
        var tool = new GitCommitTool(new PermissiveGitPolicy());
        var result = await tool.ExecuteAsync(new ToolCall("git_commit", $$"""{"path":{{JsonSerializer.Serialize(_repoDir)}}}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'message'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GitCheckout_DefaultPolicy_Blocked()
    {
        var tool = new GitCheckoutTool(new DefaultGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_checkout", new { branch = "main", path = _repoDir }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GitCheckout_InvalidCreateType_ReturnsStructuredValidationError()
    {
        var tool = new GitCheckoutTool(new PermissiveGitPolicy());
        var result = await tool.ExecuteAsync(
            new ToolCall("git_checkout", $$"""{"branch":"main","create":"yes","path":{{JsonSerializer.Serialize(_repoDir)}}}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'create'", result.Content, StringComparison.Ordinal);
    }

    // ── Write-ops with PermissiveGitPolicy ───────────────────────

    [Fact]
    public async Task GitAdd_PermissivePolicy_Succeeds()
    {
        File.WriteAllText(Path.Combine(_repoDir, "staged.txt"), "stage me");

        var tool = new GitAddTool(new PermissiveGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_add", new { files = "staged.txt", path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GitCheckout_PermissivePolicy_CreatesNewBranch()
    {
        var tool = new GitCheckoutTool(new PermissiveGitPolicy());
        var result = await tool.ExecuteAsync(Call("git_checkout", new { branch = "test-branch", create = true, path = _repoDir }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.True(output.GetProperty("created").GetBoolean());
    }

    private static ToolCall Call(string name, object args) =>
        new(name, JsonSerializer.Serialize(args));

    private static void DeleteDirectoryRobust(string path)
    {
        if (!Directory.Exists(path))
            return;

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                ClearReadOnlyAttributes(path);
                Directory.Delete(path, recursive: true);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                Thread.Sleep(100 * attempt);
            }
        }

        // Let the final attempt throw with original context if deletion still fails.
        ClearReadOnlyAttributes(path);
        Directory.Delete(path, recursive: true);
    }

    private static void ClearReadOnlyAttributes(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attrs = File.GetAttributes(file);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
            }
            catch
            {
                // Best effort for cleanup in tests.
            }
        }
    }
}
