using bashGPT.Tools.GitHub.PullRequests;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class GhPrDiffToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidPrNumber()
    {
        var tool = new GhPrDiffTool();

        var result = await tool.ExecuteAsync(
            Call("gh_pr_diff", """{"number":-1}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrDiffTool();

        var result = await tool.ExecuteAsync(
            Call("gh_pr_diff", "bad"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
