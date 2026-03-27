using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class EditFileToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_ReplacesUniqueString()
    {
        WriteFile("src/Foo.cs", "Hello World\n");
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"src/Foo.cs","old_string":"World","new_string":"Claude"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Hello Claude\n", File.ReadAllText(Path.Combine(Dir, "src/Foo.cs")));
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenOldStringNotFound()
    {
        WriteFile("a.txt", "foo\n");
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"a.txt","old_string":"MISSING","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenOldStringAmbiguous()
    {
        WriteFile("dup.txt", "abc abc\n");
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"dup.txt","old_string":"abc","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("2 times", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenFileNotFound()
    {
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"nonexistent.cs","old_string":"x","new_string":"y"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("File not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"../../etc/passwd","old_string":"root","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnMissingPath()
    {
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"old_string":"x","new_string":"y"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", "not-json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenOldStringEmpty()
    {
        WriteFile("b.txt", "content\n");
        var tool = new EditFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("edit_file", """{"path":"b.txt","old_string":"","new_string":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }
}
