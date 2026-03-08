using BashGPT.Storage;

namespace BashGPT.Core.Tests.Storage;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}");
    private string SessionsDir  => Path.Combine(_tempDir, "sessions");
    private string LegacyFile   => Path.Combine(_tempDir, "sessions.json");

    public SessionStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SessionStore CreateStore(string? legacyHistoryFile = null, string? legacySessionsFile = null)
        => new(SessionsDir, legacyHistoryFile, legacySessionsFile);

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

    [Fact]
    public async Task LoadAllAsync_ReadsOnlyIndexFile_ContentFileSeparate()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", messages: [MakeMessage("user", "hallo")]));

        // index.json muss existieren, aber keine Messages enthalten
        Assert.True(File.Exists(Path.Combine(SessionsDir, "index.json")));
        // Inhalt ist im eigenen Session-Ordner
        Assert.True(File.Exists(Path.Combine(SessionsDir, "s1", "content.json")));

        var sessions = await store.LoadAllAsync();
        Assert.Empty(sessions[0].Messages);
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
    public async Task UpsertAsync_WritesIndexAndContentFile()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        Assert.True(File.Exists(Path.Combine(SessionsDir, "index.json")));
        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s1")));
        Assert.True(File.Exists(Path.Combine(SessionsDir, "s1", "content.json")));
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

    [Fact]
    public async Task LoadAsync_MissingContentFile_ReturnsNull()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        // content.json manuell löschen → LoadAsync soll null zurückgeben
        File.Delete(Path.Combine(SessionsDir, "s1", "content.json"));

        var result = await store.LoadAsync("s1");
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsync_ReadsContentFile_IncludesMessages()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1", messages: [MakeMessage("user", "Hallo")]));

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Single(loaded.Messages);
        Assert.Equal("Hallo", loaded.Messages[0].Content);
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

    [Fact]
    public async Task UpsertAsync_ExceedsMaxSessions_DeletesOldestContentFiles()
    {
        var store = CreateStore();

        for (var i = 0; i < SessionStore.MaxSessions + 3; i++)
        {
            await store.UpsertAsync(MakeSession(
                $"s{i:D2}",
                updatedAt: $"2026-01-{i + 1:D2}T00:00:00Z"));
        }

        // Die ältesten 3 Sessions sollen keine Ordner mehr haben
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s00")));
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s01")));
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s02")));
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
    public async Task DeleteAsync_RemovesSessionDir()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s1")));

        await store.DeleteAsync("s1");

        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s1")));
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

    [Fact]
    public async Task ClearAsync_RemovesAllSessionDirs()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));
        await store.UpsertAsync(MakeSession("s2"));

        await store.ClearAsync();

        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s1")));
        Assert.False(Directory.Exists(Path.Combine(SessionsDir, "s2")));
    }

    // ── Migration von sessions.json (Legacy) ──────────────────────────────────

    [Fact]
    public async Task MigrateFromLegacySessions_CreatesIndexAndContentFiles()
    {
        var legacyJson = """
            {
              "version": 1,
              "sessions": [
                { "id": "s1", "title": "Erste", "createdAt": "2026-01-01T00:00:00Z", "updatedAt": "2026-01-01T00:00:00Z",
                  "messages": [{ "role": "user", "content": "Hallo" }] },
                { "id": "s2", "title": "Zweite", "createdAt": "2026-01-02T00:00:00Z", "updatedAt": "2026-01-02T00:00:00Z",
                  "messages": [] }
              ]
            }
            """;
        await File.WriteAllTextAsync(LegacyFile, legacyJson);

        var store    = CreateStore(legacySessionsFile: LegacyFile);
        var sessions = await store.LoadAllAsync();

        Assert.Equal(2, sessions.Count);
        Assert.True(File.Exists(Path.Combine(SessionsDir, "index.json")));
        Assert.True(File.Exists(Path.Combine(SessionsDir, "s1", "content.json")));
        Assert.True(File.Exists(Path.Combine(SessionsDir, "s2", "content.json")));
    }

    [Fact]
    public async Task MigrateFromLegacySessions_PreservesMessages()
    {
        var legacyJson = """
            {
              "version": 1,
              "sessions": [
                { "id": "s1", "title": "Erste", "createdAt": "2026-01-01T00:00:00Z", "updatedAt": "2026-01-01T00:00:00Z",
                  "messages": [{ "role": "user", "content": "Migrierter Inhalt" }] }
              ]
            }
            """;
        await File.WriteAllTextAsync(LegacyFile, legacyJson);

        var store  = CreateStore(legacySessionsFile: LegacyFile);
        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Single(loaded.Messages);
        Assert.Equal("Migrierter Inhalt", loaded.Messages[0].Content);
    }

    [Fact]
    public async Task MigrateFromLegacySessions_RenamesOldFile()
    {
        var legacyJson = """
            { "version": 1, "sessions": [] }
            """;
        await File.WriteAllTextAsync(LegacyFile, legacyJson);

        var store = CreateStore(legacySessionsFile: LegacyFile);
        await store.LoadAllAsync();

        Assert.False(File.Exists(LegacyFile));
        Assert.True(File.Exists(LegacyFile + ".migrated"));
    }

    [Fact]
    public async Task MigrateFromLegacySessions_IsIdempotent()
    {
        var legacyJson = """
            {
              "version": 1,
              "sessions": [
                { "id": "s1", "title": "T", "createdAt": "2026-01-01T00:00:00Z", "updatedAt": "2026-01-01T00:00:00Z",
                  "messages": [] }
              ]
            }
            """;
        await File.WriteAllTextAsync(LegacyFile, legacyJson);

        // Erster Start → Migration
        var store1 = CreateStore(legacySessionsFile: LegacyFile);
        await store1.LoadAllAsync();

        // Zweiter Start → kein LegacyFile mehr, index.json existiert bereits
        var store2   = CreateStore(legacySessionsFile: LegacyFile);
        var sessions = await store2.LoadAllAsync();

        Assert.Single(sessions);
    }

    [Fact]
    public async Task MigrateFromLegacySessions_MissingFile_ReturnsEmpty()
    {
        // LegacyFile existiert nicht
        var store    = CreateStore(legacySessionsFile: LegacyFile);
        var sessions = await store.LoadAllAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task MigrateFromLegacySessions_CorruptFile_ReturnsEmpty()
    {
        await File.WriteAllTextAsync(LegacyFile, "das ist kein json {{{");

        var store    = CreateStore(legacySessionsFile: LegacyFile);
        var sessions = await store.LoadAllAsync();
        Assert.Empty(sessions);
    }

    // ── Migration von history.json ────────────────────────────────────────────

    [Fact]
    public async Task LoadAllAsync_WithLegacyHistoryFile_MigratesMessages()
    {
        var legacyHistoryFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyHistoryFile, """
            [
              { "role": "user",      "content": "Hallo Welt" },
              { "role": "assistant", "content": "Hallo zurück!" }
            ]
            """);

        var store    = CreateStore(legacyHistoryFile: legacyHistoryFile);
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
        var legacyHistoryFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyHistoryFile, "[]");

        var store = CreateStore(legacyHistoryFile: legacyHistoryFile);
        Assert.Empty(await store.LoadAllAsync());
    }

    [Fact]
    public async Task LoadAllAsync_MigrationRunsOnlyOnce_NoSessionsDuplicated()
    {
        var legacyHistoryFile = Path.Combine(_tempDir, "history.json");
        await File.WriteAllTextAsync(legacyHistoryFile, """
            [{ "role": "user", "content": "Test" }]
            """);

        var store = CreateStore(legacyHistoryFile: legacyHistoryFile);

        // Erster Aufruf → Migration
        await store.LoadAllAsync();
        // Zweiter Aufruf → index.json existiert bereits, keine erneute Migration
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

    [Fact]
    public async Task UpsertAsync_ConcurrentWritesDifferentSessions_NoDataLoss()
    {
        var store = CreateStore();

        await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(i => store.UpsertAsync(MakeSession($"s{i}", messages: [MakeMessage("user", $"msg{i}")]))));

        for (var i = 0; i < 5; i++)
        {
            var loaded = await store.LoadAsync($"s{i}");
            Assert.NotNull(loaded);
            Assert.Single(loaded.Messages);
        }
    }

    // ── SaveRequestAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveRequestAsync_CreatesRequestsDir()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z"));

        Assert.True(Directory.Exists(Path.Combine(SessionsDir, "s1", "requests")));
    }

    [Fact]
    public async Task SaveRequestAsync_WritesFileWithTimestampName()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z"));

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Single(files);
        Assert.Contains("2026-03-08T15-30-00.000Z", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task SaveRequestAsync_PreservesContent()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        var record = new SessionRequestRecord
        {
            Timestamp = "2026-03-08T15:30:00.000Z",
            Request   = new SessionRequestData  { Prompt = "Was ist die Antwort?", ExecMode = "noExec" },
            Response  = new SessionResponseData { Content = "42." },
        };
        await store.SaveRequestAsync("s1", record);

        var file    = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json").Single();
        var json    = await File.ReadAllTextAsync(file);
        Assert.Contains("Was ist die Antwort?", json);
        Assert.Contains("42.", json);
    }

    [Fact]
    public async Task SaveRequestAsync_MultipleRequests_AllFilesSaved()
    {
        var store = CreateStore();
        await store.UpsertAsync(MakeSession("s1"));

        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z"));
        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:31:00.000Z"));
        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:32:00.000Z"));

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Equal(3, files.Length);
    }

    [Fact]
    public async Task SaveRequestAsync_WithoutPriorUpsert_CreatesDirectory()
    {
        var store = CreateStore();

        // Auch ohne vorherigen Upsert soll kein Fehler auftreten
        var ex = await Record.ExceptionAsync(() =>
            store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z")));
        Assert.Null(ex);
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

    private static SessionRequestRecord MakeRequest(string timestamp) => new()
    {
        Timestamp = timestamp,
        Request   = new SessionRequestData  { Prompt = "Test-Prompt" },
        Response  = new SessionResponseData { Content = "Test-Antwort" },
    };
}
