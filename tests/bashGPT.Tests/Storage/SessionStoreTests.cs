using BashGPT.Storage;

namespace BashGPT.Tests.Storage;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}");
    private string SessionsFile => Path.Combine(_tempDir, "sessions.json");

    public SessionStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SessionStore CreateStore(string? legacyHistoryFile = null)
        => new(SessionsFile, legacyHistoryFile);

    // ── LoadAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAllAsync_NoFile_ReturnsEmpty()
    {
        var store = CreateStore();
        var sessions = await store.LoadAllAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task LoadAllAsync_DoesNotIncludeMessages()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", messages: [MakeMessage("user", "hallo")]));

        var sessions = await store.LoadAllAsync();

        Assert.Single(sessions);
        Assert.Empty(sessions[0].Messages); // Messages werden nicht zurückgegeben
    }

    // ── UpsertAsync / LoadAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpsertAndLoad_RoundTrip_PreservesData()
    {
        var store = CreateStore();
        var original = MakeSession("s1", title: "Test-Session", messages: [
            MakeMessage("user",      "Was ist Zeit?"),
            MakeMessage("assistant", "Keine Ahnung."),
        ]);

        await store.UpsertAsync(original);
        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal("s1",           loaded.Id);
        Assert.Equal("Test-Session", loaded.Title);
        Assert.Equal(2,              loaded.Messages.Count);
        Assert.Equal("user",         loaded.Messages[0].Role);
        Assert.Equal("Was ist Zeit?",loaded.Messages[0].Content);
    }

    [Fact]
    public async Task UpsertAsync_ExistingId_UpdatesEntry()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", title: "Alt"));
        await store.UpsertAsync(MakeSession("s1", title: "Neu", messages: [MakeMessage("user", "x")]));

        var sessions = await store.LoadAllAsync();
        Assert.Single(sessions);

        var loaded = await store.LoadAsync("s1");
        Assert.Equal("Neu", loaded!.Title);
        Assert.Single(loaded.Messages);
    }

    [Fact]
    public async Task LoadAsync_UnknownId_ReturnsNull()
    {
        var store = CreateStore();
        var result = await store.LoadAsync("nicht-vorhanden");
        Assert.Null(result);
    }

    // ── Sortierung & MaxSessions ──────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_SortsByUpdatedAtDescending()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", updatedAt: "2026-01-01T00:00:00Z"));
        await store.UpsertAsync(MakeSession("s2", updatedAt: "2026-03-01T00:00:00Z"));
        await store.UpsertAsync(MakeSession("s3", updatedAt: "2026-02-01T00:00:00Z"));

        var sessions = await store.LoadAllAsync();

        Assert.Equal("s2", sessions[0].Id);
        Assert.Equal("s3", sessions[1].Id);
        Assert.Equal("s1", sessions[2].Id);
    }

    [Fact]
    public async Task UpsertAsync_ExceedsMaxSessions_DropsOldest()
    {
        var store = CreateStore();

        for (var i = 0; i < SessionStore.MaxSessions + 5; i++)
        {
            await store.UpsertAsync(MakeSession(
                $"s{i:D2}",
                updatedAt: $"2026-01-{i + 1:D2}T00:00:00Z"));
        }

        var sessions = await store.LoadAllAsync();
        Assert.Equal(SessionStore.MaxSessions, sessions.Count);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_KnownId_RemovesSession()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        await store.UpsertAsync(MakeSession("s2"));

        await store.DeleteAsync("s1");

        var sessions = await store.LoadAllAsync();
        Assert.Single(sessions);
        Assert.Equal("s2", sessions[0].Id);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNotThrow()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        var ex = await Record.ExceptionAsync(() => store.DeleteAsync("nicht-vorhanden"));
        Assert.Null(ex);

        Assert.Single(await store.LoadAllAsync());
    }

    // ── ClearAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearAsync_RemovesAllSessions()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        await store.UpsertAsync(MakeSession("s2"));

        await store.ClearAsync();

        Assert.Empty(await store.LoadAllAsync());
    }

    // ── Migration von history.json ────────────────────────────────────────────

    [Fact]
    public async Task LoadAllAsync_WithLegacyHistoryFile_MigratesMessages()
    {
        var legacyFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyFile, """
            [
              { "role": "user",      "content": "Hallo Welt" },
              { "role": "assistant", "content": "Hallo zurück!" }
            ]
            """);

        var store = CreateStore(legacyHistoryFile: legacyFile);
        var sessions = await store.LoadAllAsync();

        Assert.Single(sessions);
        Assert.Equal(SessionStore.LiveSessionId, sessions[0].Id);

        var loaded = await store.LoadAsync(SessionStore.LiveSessionId);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Messages.Count);
        Assert.Equal("user",      loaded.Messages[0].Role);
        Assert.Equal("Hallo Welt",loaded.Messages[0].Content);
    }

    [Fact]
    public async Task LoadAllAsync_WithEmptyLegacyFile_ReturnsEmpty()
    {
        var legacyFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyFile, "[]");

        var store = CreateStore(legacyHistoryFile: legacyFile);
        Assert.Empty(await store.LoadAllAsync());
    }

    [Fact]
    public async Task LoadAllAsync_MigrationRunsOnlyOnce_NoSessionsDuplicated()
    {
        var legacyFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyFile, """
            [{ "role": "user", "content": "Test" }]
            """);

        var store = CreateStore(legacyHistoryFile: legacyFile);

        // Erster Aufruf → Migration
        await store.LoadAllAsync();
        // Zweiter Aufruf → sessions.json existiert bereits, keine erneute Migration
        var sessions = await store.LoadAllAsync();

        Assert.Single(sessions);
    }

    // ── Parallelität (basic smoke test) ──────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_ConcurrentWrites_DoNotThrow()
    {
        var store = CreateStore();

        var tasks = Enumerable.Range(0, 10)
            .Select(i => store.UpsertAsync(MakeSession($"s{i}")));

        var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(ex);

        var sessions = await store.LoadAllAsync();
        Assert.Equal(10, sessions.Count);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static SessionRecord MakeSession(
        string id,
        string title = "Titel",
        string? updatedAt = null,
        List<SessionMessage>? messages = null) => new()
    {
        Id        = id,
        Title     = title,
        CreatedAt = "2026-01-01T00:00:00Z",
        UpdatedAt = updatedAt ?? "2026-01-01T00:00:00Z",
        Messages  = messages ?? [],
    };

    private static SessionMessage MakeMessage(string role, string content) => new()
    {
        Role    = role,
        Content = content,
    };
}
