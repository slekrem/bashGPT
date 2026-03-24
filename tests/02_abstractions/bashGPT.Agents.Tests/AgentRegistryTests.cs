using bashGPT.Agents;

namespace bashGPT.Agents.Tests;

file sealed class FakeAgent(string id, string name) : AgentBase
{
    public override string Id => id;
    public override string Name => name;
    public override IReadOnlyList<string> EnabledTools => [];
    public override IReadOnlyList<string> SystemPrompt => ["test"];
    protected override string GetAgentMarkdown(string? sessionPath = null) => $"# {name}";
}

public class AgentRegistryTests
{
    [Fact]
    public void Find_ExistingId_ReturnsAgent()
    {
        var registry = new AgentRegistry([new FakeAgent("dev", "Dev-Agent")]);

        var result = registry.Find("dev");

        Assert.NotNull(result);
        Assert.Equal("dev", result.Id);
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var registry = new AgentRegistry([new FakeAgent("dev", "Dev-Agent")]);

        Assert.NotNull(registry.Find("DEV"));
        Assert.NotNull(registry.Find("Dev"));
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        var registry = new AgentRegistry([new FakeAgent("dev", "Dev-Agent")]);

        Assert.Null(registry.Find("unknown"));
    }

    [Fact]
    public void All_ReturnsAllRegisteredAgents()
    {
        var agents = new AgentBase[]
        {
            new FakeAgent("a1", "Alpha"),
            new FakeAgent("a2", "Beta"),
        };
        var registry = new AgentRegistry(agents);

        Assert.Equal(2, registry.All.Count);
    }

    [Fact]
    public void Constructor_DuplicateId_ThrowsArgumentException()
    {
        var agents = new AgentBase[]
        {
            new FakeAgent("dev", "Dev-Agent"),
            new FakeAgent("dev", "Dev-Agent Duplicate"),
        };

        Assert.Throws<ArgumentException>(() => new AgentRegistry(agents));
    }

    [Fact]
    public void Constructor_DuplicateIdCaseInsensitive_ThrowsArgumentException()
    {
        var agents = new AgentBase[]
        {
            new FakeAgent("dev", "Dev-Agent"),
            new FakeAgent("DEV", "Dev-Agent Upper"),
        };

        Assert.Throws<ArgumentException>(() => new AgentRegistry(agents));
    }

    [Fact]
    public void Constructor_EmptyAgents_CreatesEmptyRegistry()
    {
        var registry = new AgentRegistry([]);

        Assert.Empty(registry.All);
    }
}
