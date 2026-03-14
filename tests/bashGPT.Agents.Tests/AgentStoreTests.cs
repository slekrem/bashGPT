using BashGPT.Agents;

namespace BashGPT.Agents.Tests;

file sealed class ConcreteAgent : AgentBase
{
    public override string Id => "test";
    public override string Name => "Test-Agent";
    public override IReadOnlyList<string> EnabledTools => ["tool_a", "tool_b"];
    public override string SystemPrompt => "Du bist ein Test.";
    protected override string GetAgentMarkdown() => "# Test-Agent\n\nInfo.";
}

public class AgentBaseTests
{
    [Fact]
    public void AgentBase_ConcreteImplementation_ExposesAllProperties()
    {
        var agent = new ConcreteAgent();

        Assert.Equal("test", agent.Id);
        Assert.Equal("Test-Agent", agent.Name);
        Assert.Equal(["tool_a", "tool_b"], agent.EnabledTools);
        Assert.Equal("Du bist ein Test.", agent.SystemPrompt);
    }

    [Fact]
    public void AgentBase_GetInfoPanelMarkdown_ReturnsNonEmptyString()
    {
        var agent = new ConcreteAgent();
        var md = agent.GetInfoPanelMarkdown();

        Assert.False(string.IsNullOrWhiteSpace(md));
        Assert.Contains("# Test-Agent", md, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentBase_IsAbstract()
    {
        Assert.True(typeof(AgentBase).IsAbstract);
    }
}
