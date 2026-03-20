using bashGPT.Agents;
using bashGPT.Agents.Dev;
using bashGPT.Agents.Shell;

namespace bashGPT.Agents.Tests;

file sealed class CustomAgent : AgentBase
{
    public override string Id => "custom";
    public override string Name => "Custom Agent";
    public override IReadOnlyList<string> EnabledTools => [];
    public override IReadOnlyList<string> SystemPrompt => ["You are a custom agent."];
    protected override string GetAgentMarkdown() => "# Custom Agent";
}

public class AgentArchitectureGuardTests
{
    [Fact]
    public void AllAgents_InheritFromAgentBase()
    {
        Assert.True(typeof(AgentBase).IsAssignableFrom(typeof(DevAgent)));
        Assert.True(typeof(AgentBase).IsAssignableFrom(typeof(ShellAgent)));
    }

    [Fact]
    public void AllAgents_HaveUniqueIds()
    {
        var agents = new AgentBase[] { new DevAgent(), new ShellAgent() };
        var ids = agents.Select(a => a.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void AgentRegistry_ContainsAllAgents()
    {
        var registry = new AgentRegistry([new DevAgent(), new ShellAgent()]);

        Assert.NotNull(registry.Find("dev"));
        Assert.NotNull(registry.Find("shell"));
    }

    [Fact]
    public void AgentBase_CanBeExtendedWithCustomAgent()
    {
        var agent = new CustomAgent();

        Assert.IsAssignableFrom<AgentBase>(agent);
        Assert.Equal("custom", agent.Id);
        Assert.NotEmpty(agent.SystemPrompt);
        Assert.NotNull(agent.GetInfoPanelMarkdown());
    }

    [Fact]
    public void AgentRegistry_AcceptsCustomAgent()
    {
        var registry = new AgentRegistry([new DevAgent(), new ShellAgent(), new CustomAgent()]);

        Assert.NotNull(registry.Find("custom"));
        Assert.Equal(3, registry.All.Count);
    }
}
