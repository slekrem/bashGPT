using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class ReadFileToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnEmptyPathsArray()
    {
        var tool = new ReadFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", """{"paths":[]}"""),
            CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new ReadFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", "bad json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenPathsMissing()
    {
        var tool = new ReadFileTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("read_file", "{}"),
            CancellationToken.None);

        Assert.False(result.Success);
    }
}
