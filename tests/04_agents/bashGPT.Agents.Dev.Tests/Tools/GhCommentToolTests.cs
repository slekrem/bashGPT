using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class GhCommentToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnMissingNumber()
    {
        var tool = new GhCommentTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"body":"hello"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnMissingBody()
    {
        var tool = new GhCommentTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"number":42}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidType()
    {
        var tool = new GhCommentTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", """{"number":42,"body":"hi","type":"discussion"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhCommentTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_comment", "nope"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
