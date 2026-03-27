using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class WriteFileToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_CreatesNewFile()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"new.txt","content":"hello\n"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(Dir, "new.txt")));
        Assert.Equal("hello\n", File.ReadAllText(Path.Combine(Dir, "new.txt")));
    }

    [Fact]
    public async Task ExecuteAsync_OverwritesExistingFile()
    {
        WriteFile("over.txt", "old content\n");
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"over.txt","content":"new content\n"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("new content\n", File.ReadAllText(Path.Combine(Dir, "over.txt")));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSubdirectory()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"sub/dir/file.txt","content":"x"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(Dir, "sub", "dir", "file.txt")));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"../../evil.txt","content":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnMissingPath()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"content":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", "{bad json}"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsLineCount()
    {
        var tool = new WriteFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("write_file", """{"path":"lines.txt","content":"a\nb\nc"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("3 lines", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
