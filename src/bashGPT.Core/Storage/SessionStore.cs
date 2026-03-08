using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Storage;

/// <summary>
/// Verwaltet Session-Daten im zwei-Schichten-Layout:
///   sessions/index.json      – Metadaten aller Sessions
///   sessions/&lt;id&gt;.json      – Inhalt einer einzelnen Session
///
/// Thread-safe via SemaphoreSlim, atomisches Schreiben via Temp-Datei.
/// Einmalige Migration von alter sessions.json beim ersten Zugriff.
/// </summary>
public class SessionStore
{
    public const int MaxSessions = 20;
    public const string LiveSessionId = "current";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string  _sessionsDir;
    private readonly string  _indexFile;
    private readonly string? _legacyHistoryFile;
    private readonly string? _legacySessionsFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionStore(
        string  sessionsDir,
        string? legacyHistoryFile  = null,
        string? legacySessionsFile = null)
    {
        _sessionsDir        = sessionsDir;
        _indexFile          = Path.Combine(sessionsDir, "index.json");
        _legacyHistoryFile  = legacyHistoryFile;
        _legacySessionsFile = legacySessionsFile;
    }

    // ── Öffentliche API ───────────────────────────────────────────────────────

    /// <summary>Gibt alle Sessions ohne Messages zurück (für Sidebar).</summary>
    public async Task<List<SessionRecord>> LoadAllAsync()
    {
        var index = await ReadIndexAsync();
        return index.Sessions
            .Select(e => new SessionRecord
            {
                Id        = e.Id,
                Title     = e.Title,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Messages  = [],
            })
            .ToList();
    }

    /// <summary>Lädt eine einzelne Session mit allen Messages.</summary>
    public async Task<SessionRecord?> LoadAsync(string id)
    {
        var index = await ReadIndexAsync();
        var entry = index.Sessions.FirstOrDefault(e => e.Id == id);
        if (entry is null) return null;

        var content = await ReadContentAsync(id);
        if (content is null) return null;

        return new SessionRecord
        {
            Id           = entry.Id,
            Title        = entry.Title,
            CreatedAt    = entry.CreatedAt,
            UpdatedAt    = entry.UpdatedAt,
            Messages     = content.Messages,
            ShellContext = content.ShellContext,
        };
    }

    /// <summary>
    /// Legt eine neue Session an oder aktualisiert eine bestehende (Upsert).
    /// Sortiert nach UpdatedAt, kappt bei MaxSessions inkl. Dateibereinigung.
    /// </summary>
    public async Task UpsertAsync(SessionRecord session)
    {
        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_sessionsDir);

            // 1. Einzeldatei zuerst schreiben (Crash-safe: Datei ohne Index-Eintrag ist harmlos)
            var content = new SessionContent
            {
                Messages     = session.Messages,
                ShellContext = session.ShellContext,
            };
            await WriteContentInternalAsync(session.Id, content);

            // 2. Index aktualisieren
            var index    = await ReadIndexInternalAsync();
            var existing = index.Sessions.FirstOrDefault(e => e.Id == session.Id);
            var newEntry = new SessionIndexEntry
            {
                Id        = session.Id,
                Title     = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
            };

            if (existing is not null)
                index.Sessions[index.Sessions.IndexOf(existing)] = newEntry;
            else
                index.Sessions.Insert(0, newEntry);

            // Sortieren + auf MaxSessions kürzen
            var sorted = index.Sessions
                .OrderByDescending(e => e.UpdatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .ToList();

            // Überzählige Einzeldateien löschen
            foreach (var removed in sorted.Skip(MaxSessions))
                TryDeleteContentFile(removed.Id);

            index.Sessions = [.. sorted.Take(MaxSessions)];
            await WriteIndexInternalAsync(index);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Löscht eine Session anhand ihrer ID.</summary>
    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            TryDeleteContentFile(id);

            var index = await ReadIndexInternalAsync();
            index.Sessions.RemoveAll(e => e.Id == id);
            await WriteIndexInternalAsync(index);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Löscht alle Sessions.</summary>
    public async Task ClearAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var index = await ReadIndexInternalAsync();
            foreach (var entry in index.Sessions)
                TryDeleteContentFile(entry.Id);

            await WriteIndexInternalAsync(new SessionIndex());
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Interne Hilfsmethoden ─────────────────────────────────────────────────

    private string ContentFilePath(string id) => Path.Combine(_sessionsDir, $"{id}.json");

    private async Task<SessionIndex> ReadIndexAsync()
    {
        await _lock.WaitAsync();
        try { return await ReadIndexInternalAsync(); }
        finally { _lock.Release(); }
    }

    /// <summary>Liest index.json ohne Lock. Löst bei Bedarf Migration aus.</summary>
    private async Task<SessionIndex> ReadIndexInternalAsync()
    {
        if (File.Exists(_indexFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_indexFile);
                return JsonSerializer.Deserialize<SessionIndex>(json, JsonOptions) ?? new SessionIndex();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new SessionIndex();
            }
        }

        // Migration von alter sessions.json
        if (_legacySessionsFile is not null && File.Exists(_legacySessionsFile))
            return await MigrateFromLegacySessionsFileAsync();

        // Migration von alter history.json
        if (_legacyHistoryFile is not null && File.Exists(_legacyHistoryFile))
            return await MigrateFromHistoryAsync();

        return new SessionIndex();
    }

    private async Task<SessionContent?> ReadContentAsync(string id)
    {
        var path = ContentFilePath(id);
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<SessionContent>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task WriteIndexInternalAsync(SessionIndex index)
    {
        Directory.CreateDirectory(_sessionsDir);
        var tmp  = _indexFile + ".tmp";
        var json = JsonSerializer.Serialize(index, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, _indexFile, overwrite: true);
    }

    private async Task WriteContentInternalAsync(string id, SessionContent content)
    {
        Directory.CreateDirectory(_sessionsDir);
        var path = ContentFilePath(id);
        var tmp  = path + ".tmp";
        var json = JsonSerializer.Serialize(content, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    private void TryDeleteContentFile(string id)
    {
        try { File.Delete(ContentFilePath(id)); }
        catch { /* ignorieren – Datei evtl. nicht vorhanden */ }
    }

    // ── Migration ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Einmalige Migration von alter sessions.json ins neue zwei-Schichten-Layout.
    /// Idempotent: index.json wird zuerst geschrieben, danach alte Datei umbenannt.
    /// </summary>
    private async Task<SessionIndex> MigrateFromLegacySessionsFileAsync()
    {
        try
        {
            var json    = await File.ReadAllTextAsync(_legacySessionsFile!);
            var oldFile = JsonSerializer.Deserialize<SessionsFile>(json, JsonOptions) ?? new SessionsFile();

            Directory.CreateDirectory(_sessionsDir);

            var indexEntries = new List<SessionIndexEntry>();
            foreach (var session in oldFile.Sessions)
            {
                var content = new SessionContent
                {
                    Messages     = session.Messages,
                    ShellContext = session.ShellContext,
                };
                await WriteContentInternalAsync(session.Id, content);
                indexEntries.Add(new SessionIndexEntry
                {
                    Id        = session.Id,
                    Title     = session.Title,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt,
                });
            }

            var index = new SessionIndex { Sessions = indexEntries };

            // Index zuerst schreiben, dann alte Datei umbenennen (Idempotenz)
            await WriteIndexInternalAsync(index);
            File.Move(_legacySessionsFile!, _legacySessionsFile + ".migrated", overwrite: true);

            return index;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new SessionIndex();
        }
    }

    /// <summary>
    /// Einmalige Migration von history.json → eine Live-Session im neuen Layout.
    /// </summary>
    private async Task<SessionIndex> MigrateFromHistoryAsync()
    {
        try
        {
            var legacyOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json  = await File.ReadAllTextAsync(_legacyHistoryFile!);
            var items = JsonSerializer.Deserialize<List<LegacyHistoryItem>>(json, legacyOptions) ?? [];

            if (items.Count == 0)
                return new SessionIndex();

            var messages = items
                .Where(i => i.Role is "user" or "assistant")
                .Select(i => new SessionMessage { Role = i.Role, Content = i.Content })
                .ToList();

            var title = messages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Importierter Verlauf";
            if (title.Length > 40) title = title[..40] + "…";

            var now = DateTime.UtcNow.ToString("o");
            var content = new SessionContent { Messages = messages };

            Directory.CreateDirectory(_sessionsDir);
            await WriteContentInternalAsync(LiveSessionId, content);

            var entry = new SessionIndexEntry
            {
                Id        = LiveSessionId,
                Title     = title,
                CreatedAt = now,
                UpdatedAt = now,
            };
            var index = new SessionIndex { Sessions = [entry] };
            await WriteIndexInternalAsync(index);

            return index;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new SessionIndex();
        }
    }

    private sealed record LegacyHistoryItem(string Role, string Content);
}
