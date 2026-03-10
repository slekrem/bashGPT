using BashGPT.Agents;

namespace BashGPT.Agents.Tests;

public class AgentStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AgentStore _store;

    public AgentStoreTests()
    {
        _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"agents-test-{Guid.NewGuid():N}");
        _store = new AgentStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task AgentStore_UpsertAndReload_PreservesState()
    {
        var agent = new AgentRecord
        {
            Id           = "ag-abc12345",
            Name         = "test-agent",
            SystemPrompt = "Du bist ein Test-Agent.",
            EnabledTools = ["bash", "web"],
        };

        await _store.UpsertAsync(agent);
        var reloaded = await _store.LoadAsync(agent.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(agent.Id,           reloaded.Id);
        Assert.Equal(agent.Name,         reloaded.Name);
        Assert.Equal(agent.SystemPrompt, reloaded.SystemPrompt);
        Assert.Equal(agent.EnabledTools, reloaded.EnabledTools);
    }

    [Fact]
    public async Task AgentStore_LoadByName_ReturnsCorrectAgent()
    {
        var a1 = new AgentRecord { Id = "ag-00000001", Name = "alpha" };
        var a2 = new AgentRecord { Id = "ag-00000002", Name = "beta" };

        await _store.UpsertAsync(a1);
        await _store.UpsertAsync(a2);

        var found = await _store.LoadAsync("beta");

        Assert.NotNull(found);
        Assert.Equal("ag-00000002", found.Id);
    }

    [Fact]
    public async Task AgentStore_Delete_RemovesAgent()
    {
        var agent = new AgentRecord { Id = "ag-deadbeef", Name = "to-delete" };
        await _store.UpsertAsync(agent);

        await _store.DeleteAsync(agent.Id);

        var all = await _store.LoadAllAsync();
        Assert.DoesNotContain(all, a => a.Id == agent.Id);
    }

    [Fact]
    public async Task AgentStore_LoadAll_ReturnsAllAgents()
    {
        var a1 = new AgentRecord { Id = "ag-11111111", Name = "first" };
        var a2 = new AgentRecord { Id = "ag-22222222", Name = "second" };

        await _store.UpsertAsync(a1);
        await _store.UpsertAsync(a2);

        var all = await _store.LoadAllAsync();

        Assert.Contains(all, a => a.Id == a1.Id);
        Assert.Contains(all, a => a.Id == a2.Id);
    }
}
