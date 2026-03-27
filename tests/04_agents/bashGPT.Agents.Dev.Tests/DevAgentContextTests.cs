using System.Linq;
using bashGPT.Agents.Dev;
using Xunit;

namespace bashGPT.Agents.Dev.Tests;

public class DevAgentContextTests
{
    private readonly DevAgent _agent = new();

    [Fact]
    public void BuildGitContext_ContainsBranchHeader()
    {
        var gitContext = _agent.GetContextMessages().FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(gitContext), "Git context should not be empty");

        Assert.Contains("# Git Context", gitContext);
        Assert.Contains("**Branch:**", gitContext);
    }

    [Fact]
    public void BuildGitContext_WhenUpstreamTracked_SyncLineAndDiffSectionUseCorrectFormat()
    {
        var gitContext = _agent.GetContextMessages().FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(gitContext), "Git context should not be empty");

        // Upstream sections are only rendered when a tracking branch is configured — skip otherwise.
        if (!gitContext!.Contains("→"))
            return;

        Assert.Contains("**Sync:**", gitContext);
        Assert.Contains("ahead", gitContext);
        Assert.Contains("behind upstream", gitContext);
    }

    [Fact]
    public void BuildGitContext_WhenDefaultBranchReachable_DivergenceLineUsesCorrectFormat()
    {
        var gitContext = _agent.GetContextMessages().FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(gitContext), "Git context should not be empty");

        // Default-branch divergence is only rendered when origin/HEAD resolves — skip otherwise.
        if (!gitContext!.Contains("**vs `"))
            return;

        Assert.Matches(@"\*\*vs `[^`]+`:\*\* \d+ ahead, \d+ behind", gitContext);
    }
}
