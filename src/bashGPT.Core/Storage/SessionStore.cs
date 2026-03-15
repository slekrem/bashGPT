using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
        ValidateSessionId(id);
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
            EnabledTools = content.EnabledTools,
            AgentId      = content.AgentId,
        };
    }

    /// <summary>
    /// Legt eine neue Session an oder aktualisiert eine bestehende (Upsert).
    /// Sortiert nach UpdatedAt, kappt bei MaxSessions inkl. Dateibereinigung.
    /// </summary>
    public async Task UpsertAsync(SessionRecord session)
    {
        ValidateSessionId(session.Id);
        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_sessionsDir);

            // 1. Einzeldatei zuerst schreiben (Crash-safe: Datei ohne Index-Eintrag ist harmlos)
            var content = new SessionContent
            {
                Messages     = session.Messages,
                ShellContext = session.ShellContext,
                EnabledTools = session.EnabledTools,
                AgentId      = session.AgentId,
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
                TryDeleteSessionDir(removed.Id);

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
        ValidateSessionId(id);
        await _lock.WaitAsync();
        try
        {
            TryDeleteSessionDir(id);

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
                TryDeleteSessionDir(entry.Id);

            await WriteIndexInternalAsync(new SessionIndex());
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Speichert eine Request-Dokumentation unter sessions/&lt;id&gt;/requests/&lt;timestamp&gt;.json.
    /// Kein globaler Lock nötig – jeder Request bekommt einen eindeutigen Zeitstempel.
    /// </summary>
    public async Task SaveRequestAsync(string sessionId, SessionRequestRecord record)
    {
        ValidateSessionId(sessionId);
        var dir = Path.Combine(SessionDir(sessionId), "requests");
        Directory.CreateDirectory(dir);

        // Zeitstempel als Dateiname: ISO 8601, Doppelpunkte ersetzt durch Bindestriche
        var safeName = record.Timestamp.Replace(":", "-").Replace("+", "+");
        var path     = Path.Combine(dir, safeName + ".json");
        var tmp      = path + ".tmp";
        var json     = JsonSerializer.Serialize(record, JsonOptions);

        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>
    /// Speichert den rohen LLM-Request-Body unter sessions/&lt;id&gt;/requests/&lt;timestamp&gt;-llm-request.json.
    /// Der Inhalt ist das JSON, das tatsächlich an den Provider gesendet wurde.
    /// </summary>
    public async Task SaveLlmRequestAsync(string sessionId, string timestamp, string llmRequestJson)
    {
        ValidateSessionId(sessionId);
        var dir = Path.Combine(SessionDir(sessionId), "requests");
        Directory.CreateDirectory(dir);

        var safeName = timestamp.Replace(":", "-").Replace("+", "+");
        var path     = Path.Combine(dir, safeName + "-llm-request.json");
        var tmp      = path + ".tmp";

        await File.WriteAllTextAsync(tmp, llmRequestJson);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>
    /// Speichert den rohen LLM-Response-Body unter sessions/&lt;id&gt;/requests/&lt;timestamp&gt;-llm-response.json.
    /// Der Inhalt ist das JSON (bzw. SSE-Zeilen), das der Provider zurückgeliefert hat.
    /// </summary>
    public async Task SaveLlmResponseAsync(string sessionId, string timestamp, string llmResponseJson)
    {
        ValidateSessionId(sessionId);
        var dir = Path.Combine(SessionDir(sessionId), "requests");
        Directory.CreateDirectory(dir);

        var safeName = timestamp.Replace(":", "-").Replace("+", "+");
        var path     = Path.Combine(dir, safeName + "-llm-response.json");
        var tmp      = path + ".tmp";

        await File.WriteAllTextAsync(tmp, llmResponseJson);
        File.Move(tmp, path, overwrite: true);
    }

    // ── Interne Hilfsmethoden ─────────────────────────────────────────────────

    /// <summary>
    /// Wirft ArgumentException, wenn <paramref name="id"/> nicht der erlaubten
    /// Zeichen-Whitelist entspricht oder außerhalb von <see cref="_sessionsDir"/> liegen würde.
    /// Erlaubt: Buchstaben, Ziffern, Bindestrich, Unterstrich (1–128 Zeichen).
    /// </summary>
    private static readonly Regex ValidIdPattern = new(@"^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled);

    private void ValidateSessionId(string id)
    {
        if (!ValidIdPattern.IsMatch(id))
            throw new ArgumentException($"Ungültige Session-ID: '{id}'.", nameof(id));

        var root    = Path.GetFullPath(_sessionsDir) + Path.DirectorySeparatorChar;
        var target  = Path.GetFullPath(Path.Combine(_sessionsDir, id)) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Session-ID '{id}' führt außerhalb des erlaubten Verzeichnisses.", nameof(id));
    }

    public  string GetSessionDir(string id)       => Path.Combine(_sessionsDir, id);
    private string SessionDir(string id)         => GetSessionDir(id);
    private string ContentFilePath(string id)    => Path.Combine(_sessionsDir, id, "content.json");

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
        var dir  = SessionDir(id);
        Directory.CreateDirectory(dir);
        var path = ContentFilePath(id);
        var tmp  = path + ".tmp";
        var json = JsonSerializer.Serialize(content, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    private void TryDeleteSessionDir(string id)
    {
        try { Directory.Delete(SessionDir(id), recursive: true); }
        catch { /* ignorieren – Ordner evtl. nicht vorhanden */ }
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
                    EnabledTools = session.EnabledTools,
                    AgentId      = session.AgentId,
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
