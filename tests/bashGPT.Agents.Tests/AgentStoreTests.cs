using BashGPT.Agents;

namespace BashGPT.Agents.Tests;

public class AgentStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly AgentStore _store;

    public AgentStoreTests()
    {
        _tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"agents-test-{Guid.NewGuid():N}.json");
        _store = new AgentStore(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (File.Exists(_tempFile + ".tmp")) File.Delete(_tempFile + ".tmp");
    }

    [Fact]
    public async Task AgentStore_UpsertAndReload_PreservesState()
    {
        var agent = new AgentRecord
        {
            Id = "ag-abc12345",
            Name = "test-agent",
            Type = AgentCheckType.GitStatus,
            Path = "/some/path",
            IntervalSeconds = 15,
            IsActive = true,
            LastHash = "abc123",
            LastMessage = "Alles gut",
            FailureCount = 2,
            LastCheckSucceeded = false,
        };

        await _store.UpsertAsync(agent);
        var reloaded = await _store.LoadAsync(agent.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(agent.Id, reloaded.Id);
        Assert.Equal(agent.Name, reloaded.Name);
        Assert.Equal(agent.Type, reloaded.Type);
        Assert.Equal(agent.Path, reloaded.Path);
        Assert.Equal(agent.IntervalSeconds, reloaded.IntervalSeconds);
        Assert.Equal(agent.LastHash, reloaded.LastHash);
        Assert.Equal(agent.LastMessage, reloaded.LastMessage);
        Assert.Equal(agent.FailureCount, reloaded.FailureCount);
        Assert.Equal(agent.LastCheckSucceeded, reloaded.LastCheckSucceeded);
    }

    [Fact]
    public async Task AgentStore_LoadByName_ReturnsCorrectAgent()
    {
        var a1 = new AgentRecord { Id = "ag-00000001", Name = "alpha", Type = AgentCheckType.GitStatus };
        var a2 = new AgentRecord { Id = "ag-00000002", Name = "beta", Type = AgentCheckType.HttpStatus, Url = "http://example.com" };

        await _store.UpsertAsync(a1);
        await _store.UpsertAsync(a2);

        var found = await _store.LoadAsync("beta");

        Assert.NotNull(found);
        Assert.Equal("ag-00000002", found.Id);
        Assert.Equal(AgentCheckType.HttpStatus, found.Type);
    }

    [Fact]
    public async Task AgentStore_Delete_RemovesAgent()
    {
        var agent = new AgentRecord { Id = "ag-deadbeef", Name = "to-delete", Type = AgentCheckType.GitStatus };
        await _store.UpsertAsync(agent);

        await _store.DeleteAsync(agent.Id);

        var all = await _store.LoadAllAsync();
        Assert.DoesNotContain(all, a => a.Id == agent.Id);
    }
}
