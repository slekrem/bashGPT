using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class SearchToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FindsMatchInFile()
    {
        WriteFile("code.cs", "public class Foo { }\n");
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"Foo"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("code.cs", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_IsCaseInsensitive()
    {
        WriteFile("file.txt", "Hello World\n");
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"hello world"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("file.txt", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWhenNoMatches()
    {
        WriteFile("empty.txt", "nothing here\n");
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"XYZNOTFOUND"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("No matches", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsMaxResults()
    {
        var lines = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"match line {i}"));
        WriteFile("many.txt", lines + "\n");
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"match","max_results":3}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("limited to 3", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsPathTraversal()
    {
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"x","path":"../../etc"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Path traversal", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnMissingQuery()
    {
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"max_results":5}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", "!!!"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_SearchesWithinSubPath()
    {
        WriteFile("sub/match.txt", "target\n");
        WriteFile("other.txt", "target\n");
        var tool = new SearchTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("search", """{"query":"target","path":"sub"}"""),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("sub/match.txt", result.Content.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
