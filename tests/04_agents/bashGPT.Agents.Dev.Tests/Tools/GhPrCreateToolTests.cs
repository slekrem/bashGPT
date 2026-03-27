using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class GhPrCreateToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnMissingTitle()
    {
        var tool = new GhPrCreateTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_create", """{"body":"description"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrCreateTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("gh_pr_create", "bad"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
