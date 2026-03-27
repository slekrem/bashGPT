using System.Linq;
using bashGPT.Agents.Dev;
using Xunit;

namespace bashGPT.Agents.Dev.Tests;

public class DevAgentContextTests
{
    private readonly DevAgent _agent = new();

    [Fact]
    public void BuildGitContext_IncludesDiffStatsSections()
    {
        // Get the Git context string (first element of context messages)
        var gitContext = _agent.GetContextMessages().FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(gitContext), "Git context should not be empty");

        // The enriched context should contain diff sections for upstream and default branch
        Assert.Contains("**Diff vs upstream:**", gitContext);
        // The default branch diff heading includes the branch name in backticks
        Assert.Contains("**Diff vs `", gitContext);
    }
}
