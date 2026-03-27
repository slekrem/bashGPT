using bashGPT.Agents.Dev.Tools;

namespace bashGPT.Agents.Dev.Tests.Tools;

public class GitCommitToolTests : ToolTestBase
{
    [Fact]
    public async Task ExecuteAsync_FailsOnMissingMessage()
    {
        var tool = new GitCommitTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("git_commit", """{"paths":["foo.cs"]}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnInvalidJson()
    {
        var tool = new GitCommitTool(Dir);

        var result = await tool.ExecuteAsync(
            Call("git_commit", "nope"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
