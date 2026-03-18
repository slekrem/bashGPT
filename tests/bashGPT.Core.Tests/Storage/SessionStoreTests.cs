using bashGPT.Core.Storage;
using BashGPT.Storage;

namespace BashGPT.Core.Tests.Storage;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}");

    private string SessionsDir => Path.Combine(_tempDir, "sessions");

    public SessionStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SessionStore CreateStore() => new(SessionsDir);

    [Fact]
    public async Task LoadAllAsync_NoIndexFile_ReturnsEmpty()
    {
        var store = CreateStore();

        var sessions = await store.LoadAllAsync();

        Assert.Empty(sessions);
    }

    [Fact]
    public async Task LoadAllAsync_ReturnsSessionSummariesWithoutMessages()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", messages: [MakeMessage("user", "hello")]));

        var sessions = await store.LoadAllAsync();

        Assert.Single(sessions);
        Assert.Empty(sessions[0].Messages);
    }

    [Fact]
    public async Task UpsertAndLoadAsync_RoundTrip_PreservesSessionData()
    {
        var store = CreateStore();
        var original = MakeSession(
            "s1",
            title: "Test Session",
            messages:
            [
                MakeMessage("user", "What time is it?"),
                MakeMessage("assistant", "No idea."),
            ]);

        await store.UpsertAsync(original);

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal("s1", loaded!.Id);
        Assert.Equal("Test Session", loaded.Title);
        Assert.Equal(2, loaded.Messages.Count);
        Assert.Equal("What time is it?", loaded.Messages[0].Content);
    }

    [Fact]
    public async Task UpsertAsync_WritesIndexAndContentFiles()
    {
        var store = CreateStore();

        await store.UpsertAsync(MakeSession("s1"));

        Assert.True(File.Exists(Path.Combine(SessionsDir, "index.json")));
        Assert.True(File.Exists(Path.Combine(SessionsDir, "s1", "content.json")));
    }

    [Fact]
    public async Task UpsertAsync_ExistingId_UpdatesStoredEntry()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", title: "Old"));
        await store.UpsertAsync(MakeSession("s1", title: "New", messages: [MakeMessage("user", "x")]));

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal("New", loaded!.Title);
        Assert.Single(loaded.Messages);
    }

    [Fact]
    public async Task LoadAsync_UnknownId_ReturnsNull()
    {
        var store = CreateStore();

        var session = await store.LoadAsync("missing");

        Assert.Null(session);
    }

    [Fact]
    public async Task LoadAsync_MissingContentFile_ReturnsNull()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        File.Delete(Path.Combine(SessionsDir, "s1", "content.json"));

        var session = await store.LoadAsync("s1");

        Assert.Null(session);
    }

    [Fact]
    public async Task UpsertAsync_SortsByUpdatedAtDescending()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", updatedAt: "2026-01-01T00:00:00Z"));
        await store.UpsertAsync(MakeSession("s2", updatedAt: "2026-03-01T00:00:00Z"));
        await store.UpsertAsync(MakeSession("s3", updatedAt: "2026-02-01T00:00:00Z"));

        var sessions = await store.LoadAllAsync();

        Assert.Equal(["s2", "s3", "s1"], sessions.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task UpsertAsync_PersistsAllSessionsWithoutTrimming()
    {
        var store = CreateStore();
        const int sessionCount = 23;

        for (var i = 0; i < sessionCount; i++)
        {
            await store.UpsertAsync(MakeSession(
                $"s{i:D2}",
                updatedAt: $"2026-01-{i + 1:D2}T00:00:00Z"));
        }

        var sessions = await store.LoadAllAsync();

        Assert.Equal(sessionCount, sessions.Count);
        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s00")));
        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s01")));
        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s02")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesSessionAndDirectory()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        await store.UpsertAsync(MakeSession("s2"));

        await store.DeleteAsync("s1");

        var sessions = await store.LoadAllAsync();
        Assert.Single(sessions);
        Assert.Equal("s2", sessions[0].Id);
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s1")));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllSessionsAndDirectories()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        await store.UpsertAsync(MakeSession("s2"));

        await store.ClearAsync();

        Assert.Empty(await store.LoadAllAsync());
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s1")));
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s2")));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../etc/passwd")]
    [InlineData("../../secret")]
    [InlineData("s1/../../etc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc def")]
    [InlineData("abc/def")]
    [InlineData("abc\\def")]
    public async Task LoadAsync_InvalidSessionId_ThrowsArgumentException(string invalidId)
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentException>(() => store.LoadAsync(invalidId));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../evil")]
    public async Task DeleteAsync_InvalidSessionId_ThrowsArgumentException(string invalidId)
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentException>(() => store.DeleteAsync(invalidId));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../evil")]
    public async Task SaveRequestAsync_InvalidSessionId_ThrowsArgumentException(string invalidId)
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveRequestAsync(invalidId, MakeRequest("2026-03-08T15:30:00.000Z")));
    }

    [Fact]
    public async Task UpsertAsync_ConcurrentWrites_DoNotThrowOrLoseSessions()
    {
        var store = CreateStore();

        var ex = await Record.ExceptionAsync(() => Task.WhenAll(
            Enumerable.Range(0, 10).Select(i => store.UpsertAsync(MakeSession($"s{i}")))));

        Assert.Null(ex);
        Assert.Equal(10, (await store.LoadAllAsync()).Count);
    }

    [Fact]
    public async Task SaveRequestAsync_CreatesRequestFileWithTimestampName()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z"));

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Single(files);
        Assert.Contains("2026-03-08T15-30-00.000Z.json", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task SaveLlmRequestAsync_PreservesRawContent()
    {
        var store = CreateStore();
        const string llmJson = "{\"model\":\"llama3\",\"messages\":[]}";
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveLlmRequestAsync("s1", "2026-03-08T15:30:00.000Z", llmJson);

        var file = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*-llm-request.json").Single();
        var content = await File.ReadAllTextAsync(file);
        Assert.Equal(llmJson, content);
    }

    [Fact]
    public async Task SaveLlmResponseAsync_PreservesRawContent()
    {
        var store = CreateStore();
        const string raw = "data: {\"choices\":[{\"delta\":{\"content\":\"Hallo\"}}]}\ndata: [DONE]";
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveLlmResponseAsync("s1", "2026-03-08T15:30:00.000Z", raw);

        var file = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*-llm-response.json").Single();
        var content = await File.ReadAllTextAsync(file);
        Assert.Equal(raw, content);
    }

    [Fact]
    public async Task SaveRequestArtifacts_UseSeparateFiles()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        const string timestamp = "2026-03-08T15:30:00.000Z";
        await store.SaveRequestAsync("s1", MakeRequest(timestamp));
        await store.SaveLlmRequestAsync("s1", timestamp, "{\"model\":\"test\"}");
        await store.SaveLlmResponseAsync("s1", timestamp, "data: {}");

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Equal(3, files.Length);
        Assert.Single(files, f => !f.Contains("-llm"));
        Assert.Single(files, f => f.EndsWith("-llm-request.json"));
        Assert.Single(files, f => f.EndsWith("-llm-response.json"));
    }

    [Fact]
    public async Task UpsertAsync_WithEnabledTools_PersistsTools()
    {
        var store = CreateStore();
        var session = MakeSession("s1");
        session.EnabledTools = ["bash", "git_status"];

        await store.UpsertAsync(session);

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal(["bash", "git_status"], loaded!.EnabledTools);
    }

    [Fact]
    public async Task UpsertAsync_WithAgentId_PersistsAgentId()
    {
        var store = CreateStore();
        var session = MakeSession("s1");
        session.AgentId = "agent-a";

        await store.UpsertAsync(session);

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal("agent-a", loaded!.AgentId);
    }

    private static SessionRecord MakeSession(
        string id,
        string title = "Title",
        string? updatedAt = null,
        List<SessionMessage>? messages = null) => new()
    {
        Id = id,
        Title = title,
        CreatedAt = "2026-01-01T00:00:00Z",
        UpdatedAt = updatedAt ?? "2026-01-01T00:00:00Z",
        Messages = messages ?? [],
    };

    private static SessionMessage MakeMessage(string role, string content) => new()
    {
        Role = role,
        Content = content,
    };

    private static SessionRequestRecord MakeRequest(string timestamp) => new()
    {
        Timestamp = timestamp,
        Request = new SessionRequestData { Prompt = "Test prompt" },
        Response = new SessionResponseData { Content = "Test response" },
    };
}
