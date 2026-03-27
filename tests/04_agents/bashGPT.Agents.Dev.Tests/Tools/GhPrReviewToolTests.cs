using bashGPT.Tools.GitHub;
using bashGPT.Tools.GitHub.PullRequests;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class GhPrReviewToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnMissingEvent()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"body":"looks good"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidEvent()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"merge"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenBodyMissingForRequestChanges()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"request-changes"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("body", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenBodyMissingForComment()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", """{"event":"comment"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("body", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());

        var result = await tool.ExecuteAsync(
            Call("gh_pr_review", "not-json"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
