using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;

namespace BashGPT.Agents.Tests;

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
}
