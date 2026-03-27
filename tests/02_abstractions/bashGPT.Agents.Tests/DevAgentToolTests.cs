using bashGPT.Agents.Dev.Tools;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Tests;

/// <summary>
/// Tests for the built-in DevAgent tools:
/// EditFileTool, WriteFileTool, SearchTool, ReadFileTool (pure logic),
/// and argument-validation for GitCommitTool, GhPrDiffTool, GhPrReviewTool,
/// GhCommentTool, and GhPrCreateTool.
/// </summary>
public class DevAgentToolTests : IDisposable
{
    private readonly string _dir;

    public DevAgentToolTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"bashgpt-tool-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    // ── helpers ────────────────────────────────────────────────────────────────

    private string Write(string relativePath, string content)
    {
        var full = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return relativePath;
    }

    private static ToolCall Call(string name, string json) =>
        new(name, json, SessionPath: null);

    // ══════════════════════════════════════════════════════════════════════════
    // EditFileTool
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EditFileTool_ExecuteAsync_ReplacesUniqueString()
    {
        Write("src/Foo.cs", "Hello World\n");
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"src/Foo.cs","old_string":"World","new_string":"Claude"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Hello Claude\n", File.ReadAllText(Path.Combine(_dir, "src/Foo.cs")));
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsWhenOldStringNotFound()
    {
        Write("a.txt", "foo\n");
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"a.txt","old_string":"MISSING","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsWhenOldStringAmbiguous()
    {
        Write("dup.txt", "abc abc\n");
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"dup.txt","old_string":"abc","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("2 times", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsWhenFileNotFound()
    {
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"nonexistent.cs","old_string":"x","new_string":"y"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("File not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"../../etc/passwd","old_string":"root","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsOnMissingPath()
    {
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"old_string":"x","new_string":"y"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", "not-json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditFileTool_ExecuteAsync_FailsWhenOldStringEmpty()
    {
        Write("b.txt", "content\n");
        var tool = new EditFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"b.txt","old_string":"","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // WriteFileTool
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_CreatesNewFile()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"new.txt","content":"hello\n"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(_dir, "new.txt")));
        Assert.Equal("hello\n", File.ReadAllText(Path.Combine(_dir, "new.txt")));
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_OverwritesExistingFile()
    {
        Write("over.txt", "old content\n");
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"over.txt","content":"new content\n"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("new content\n", File.ReadAllText(Path.Combine(_dir, "over.txt")));
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_CreatesSubdirectory()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"sub/dir/file.txt","content":"x"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(_dir, "sub", "dir", "file.txt")));
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"../../evil.txt","content":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_FailsOnMissingPath()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"content":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", "{bad json}"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteFileTool_ExecuteAsync_ReportsLineCount()
    {
        var tool = new WriteFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"lines.txt","content":"a\nb\nc"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("3 lines", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SearchTool
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchTool_ExecuteAsync_FindsMatchInFile()
    {
        Write("code.cs", "public class Foo { }\n");
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"Foo"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("code.cs", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_IsCaseInsensitive()
    {
        Write("file.txt", "Hello World\n");
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"hello world"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("file.txt", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_ReturnsSuccessWhenNoMatches()
    {
        Write("empty.txt", "nothing here\n");
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"XYZNOTFOUND"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("No matches", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_RespectsMaxResults()
    {
        // Write a file with 10 matching lines
        var lines = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"match line {i}"));
        Write("many.txt", lines + "\n");
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"match","max_results":3}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("limited to 3", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"x","path":"../../etc"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_FailsOnMissingQuery()
    {
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"max_results":5}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", "!!!"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchTool_ExecuteAsync_SearchesWithinSubPath()
    {
        Write("sub/match.txt", "target\n");
        Write("other.txt", "target\n");
        var tool = new SearchTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"target","path":"sub"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("sub/match.txt", result.Content.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ReadFileTool — argument validation (no git required)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ReadFileTool_ExecuteAsync_FailsOnEmptyPathsArray()
    {
        var tool = new ReadFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", """{"paths":[]}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReadFileTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new ReadFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", "bad json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadFileTool_ExecuteAsync_FailsWhenPathsMissing()
    {
        var tool = new ReadFileTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", """{}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GitCommitTool — argument validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GitCommitTool_ExecuteAsync_FailsOnMissingMessage()
    {
        var tool = new GitCommitTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("git_commit", """{"paths":["foo.cs"]}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GitCommitTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GitCommitTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("git_commit", "nope"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GhPrDiffTool — argument validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GhPrDiffTool_ExecuteAsync_FailsOnInvalidPrNumber()
    {
        var tool = new GhPrDiffTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_diff", """{"number":-1}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrDiffTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrDiffTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_diff", "bad"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GhPrReviewTool — argument validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GhPrReviewTool_ExecuteAsync_FailsOnMissingEvent()
    {
        var tool = new GhPrReviewTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"body":"looks good"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrReviewTool_ExecuteAsync_FailsOnInvalidEvent()
    {
        var tool = new GhPrReviewTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"merge"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrReviewTool_ExecuteAsync_FailsWhenBodyMissingForRequestChanges()
    {
        var tool = new GhPrReviewTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"request-changes"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("body", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrReviewTool_ExecuteAsync_FailsWhenBodyMissingForComment()
    {
        var tool = new GhPrReviewTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"comment"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("body", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrReviewTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrReviewTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", "not-json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GhCommentTool — argument validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GhCommentTool_ExecuteAsync_FailsOnMissingNumber()
    {
        var tool = new GhCommentTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"body":"hello"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhCommentTool_ExecuteAsync_FailsOnMissingBody()
    {
        var tool = new GhCommentTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"number":42}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhCommentTool_ExecuteAsync_FailsOnInvalidType()
    {
        var tool = new GhCommentTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"number":42,"body":"hi","type":"discussion"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhCommentTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhCommentTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", "nope"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GhPrCreateTool — argument validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GhPrCreateTool_ExecuteAsync_FailsOnMissingTitle()
    {
        var tool = new GhPrCreateTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_create", """{"body":"description"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrCreateTool_ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrCreateTool(_dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_create", "bad"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
