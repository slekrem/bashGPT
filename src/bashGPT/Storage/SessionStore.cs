using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Storage;

/// <summary>
/// Verwaltet Session-Daten in ~/.config/bashgpt/sessions.json.
/// Thread-safe via SemaphoreSlim, atomisches Schreiben via Temp-Datei.
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

    private readonly string _sessionsFile;
    private readonly string? _legacyHistoryFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionStore(string sessionsFile, string? legacyHistoryFile = null)
    {
        _sessionsFile = sessionsFile;
        _legacyHistoryFile = legacyHistoryFile;
    }

    // ── Öffentliche API ───────────────────────────────────────────────────────

    /// <summary>Gibt alle Sessions ohne Messages zurück (für Sidebar).</summary>
    public async Task<List<SessionRecord>> LoadAllAsync()
    {
        var file = await ReadFileAsync();
        return file.Sessions
            .Select(s => new SessionRecord
            {
                Id        = s.Id,
                Title     = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                ShellContext = s.ShellContext,
                Messages  = [],          // Nachrichten weglassen – nur Metadaten
            })
            .ToList();
    }

    /// <summary>Lädt eine einzelne Session mit allen Messages.</summary>
    public async Task<SessionRecord?> LoadAsync(string id)
    {
        var file = await ReadFileAsync();
        return file.Sessions.FirstOrDefault(s => s.Id == id);
    }

    /// <summary>
    /// Legt eine neue Session an oder aktualisiert eine bestehende (Upsert).
    /// Sortiert nach UpdatedAt, kappt bei MaxSessions.
    /// </summary>
    public async Task UpsertAsync(SessionRecord session)
    {
        await _lock.WaitAsync();
        try
        {
            var file = await ReadFileInternalAsync();
            var idx = file.Sessions.FindIndex(s => s.Id == session.Id);

            if (idx >= 0)
                file.Sessions[idx] = session;
            else
                file.Sessions.Insert(0, session);

            file.Sessions = [.. file.Sessions
                .OrderByDescending(s => s.UpdatedAt)
                .ThenByDescending(s => s.CreatedAt)
                .Take(MaxSessions)];

            await WriteFileInternalAsync(file);
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
            var file = await ReadFileInternalAsync();
            file.Sessions.RemoveAll(s => s.Id == id);
            await WriteFileInternalAsync(file);
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
            await WriteFileInternalAsync(new SessionsFile());
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Interne Hilfsmethoden ─────────────────────────────────────────────────

    /// <summary>Liest die Datei (mit Lock – für öffentliche Lesemethoden).</summary>
    private async Task<SessionsFile> ReadFileAsync()
    {
        await _lock.WaitAsync();
        try { return await ReadFileInternalAsync(); }
        finally { _lock.Release(); }
    }

    /// <summary>Liest sessions.json ohne Lock (muss innerhalb von _lock aufgerufen werden).</summary>
    private async Task<SessionsFile> ReadFileInternalAsync()
    {
        if (!File.Exists(_sessionsFile))
            return await MigrateFromHistoryAsync();

        try
        {
            var json = await File.ReadAllTextAsync(_sessionsFile);
            return JsonSerializer.Deserialize<SessionsFile>(json, JsonOptions) ?? new SessionsFile();
        }
        catch
        {
            return new SessionsFile();
        }
    }

    /// <summary>
    /// Schreibt atomisch: erst in Temp-Datei, dann umbenennen.
    /// Ohne Lock – muss vom Aufrufer gesichert werden.
    /// </summary>
    private async Task WriteFileInternalAsync(SessionsFile file)
    {
        var dir = Path.GetDirectoryName(_sessionsFile)!;
        Directory.CreateDirectory(dir);

        var tmp = _sessionsFile + ".tmp";
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, _sessionsFile, overwrite: true);
    }

    /// <summary>
    /// Einmalige Migration: liest history.json und legt daraus eine Live-Session an.
    /// Gibt eine leere SessionsFile zurück wenn keine history.json vorhanden.
    /// </summary>
    private async Task<SessionsFile> MigrateFromHistoryAsync()
    {
        if (_legacyHistoryFile is null || !File.Exists(_legacyHistoryFile))
            return new SessionsFile();

        try
        {
            var legacyOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var json = await File.ReadAllTextAsync(_legacyHistoryFile);
            var items = JsonSerializer.Deserialize<List<LegacyHistoryItem>>(json, legacyOptions) ?? [];

            if (items.Count == 0)
                return new SessionsFile();

            var messages = items
                .Where(i => i.Role is "user" or "assistant")
                .Select(i => new SessionMessage { Role = i.Role, Content = i.Content })
                .ToList();

            var title = messages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Importierter Verlauf";
            if (title.Length > 40) title = title[..40] + "…";

            var now = DateTime.UtcNow.ToString("o");
            var liveSession = new SessionRecord
            {
                Id        = LiveSessionId,
                Title     = title,
                CreatedAt = now,
                UpdatedAt = now,
                Messages  = messages,
            };

            var file = new SessionsFile { Sessions = [liveSession] };
            await WriteFileInternalAsync(file);
            return file;
        }
        catch
        {
            return new SessionsFile();
        }
    }

    private sealed record LegacyHistoryItem(string Role, string Content);
}
