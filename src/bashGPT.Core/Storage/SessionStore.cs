using System.Text.Json;
using System.Text.Json.Serialization;
using bashGPT.Core.Models.Storage;

namespace bashGPT.Core.Storage;

/// <summary>
/// Manages session data in a two-layer layout:
///   sessions/index.json          - metadata for all sessions
///   sessions/&lt;id&gt;/content.json - content for a single session
///
/// Thread-safe via <see cref="SemaphoreSlim"/> and atomic writes via temp files.
/// </summary>
public class SessionStore
{
    public const string LiveSessionId = "current";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _sessionsDir;
    private readonly string _indexFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionStore(string sessionsDir)
    {
        _sessionsDir = sessionsDir;
        _indexFile = Path.Combine(sessionsDir, "index.json");
    }

    /// <summary>Returns all sessions without messages, suitable for sidebar summaries.</summary>
    public async Task<List<SessionRecord>> LoadAllAsync()
    {
        var index = await ReadIndexAsync();
        return index.Sessions
            .Select(e => new SessionRecord
            {
                Id = e.Id,
                Title = e.Title,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Messages = [],
            })
            .ToList();
    }

    /// <summary>Loads a single session including its messages.</summary>
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
            Id = entry.Id,
            Title = entry.Title,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            Messages = content.Messages,
            ShellContext = content.ShellContext,
            EnabledTools = content.EnabledTools,
            AgentId = content.AgentId,
        };
    }

    /// <summary>
    /// Creates or updates a session.
    /// Sorts sessions by <c>UpdatedAt</c> after each write.
    /// </summary>
    public async Task UpsertAsync(SessionRecord session)
    {
        ValidateSessionId(session.Id);
        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_sessionsDir);

            var content = new SessionContent
            {
                Messages = session.Messages,
                ShellContext = session.ShellContext,
                EnabledTools = session.EnabledTools,
                AgentId = session.AgentId,
            };
            await WriteContentInternalAsync(session.Id, content);

            var index = await ReadIndexInternalAsync();
            var existing = index.Sessions.FirstOrDefault(e => e.Id == session.Id);
            var newEntry = new SessionIndexEntry
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
            };

            if (existing is not null)
                index.Sessions[index.Sessions.IndexOf(existing)] = newEntry;
            else
                index.Sessions.Insert(0, newEntry);

            var sorted = index.Sessions
                .OrderByDescending(e => e.UpdatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .ToList();

            index.Sessions = sorted;
            await WriteIndexInternalAsync(index);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Deletes a session by ID.</summary>
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

    /// <summary>Deletes all sessions.</summary>
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

    private void ValidateSessionId(string id)
        => SessionStoragePaths.ValidateSessionId(_sessionsDir, id);

    public string GetSessionDir(string id) => SessionStoragePaths.GetSessionDir(_sessionsDir, id);
    private string SessionDir(string id) => GetSessionDir(id);
    private string ContentFilePath(string id) => SessionStoragePaths.GetContentFilePath(_sessionsDir, id);

    private async Task<SessionIndex> ReadIndexAsync()
    {
        await _lock.WaitAsync();
        try { return await ReadIndexInternalAsync(); }
        finally { _lock.Release(); }
    }

    private async Task<SessionIndex> ReadIndexInternalAsync()
    {
        if (!File.Exists(_indexFile))
            return new SessionIndex();

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
        var tmp = _indexFile + ".tmp";
        var json = JsonSerializer.Serialize(index, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, _indexFile, overwrite: true);
    }

    private async Task WriteContentInternalAsync(string id, SessionContent content)
    {
        var dir = SessionDir(id);
        Directory.CreateDirectory(dir);
        var path = ContentFilePath(id);
        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(content, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    private void TryDeleteSessionDir(string id)
    {
        try { Directory.Delete(SessionDir(id), recursive: true); }
        catch { }
    }
}
