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

    private readonly string _sessionsDir;
    private readonly SessionIndexStore _indexStore;
    private readonly SessionContentStore _contentStore;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionStore(string sessionsDir)
    {
        _sessionsDir = sessionsDir;
        _indexStore = new SessionIndexStore(sessionsDir);
        _contentStore = new SessionContentStore(sessionsDir);
    }

    /// <summary>Returns all sessions without messages, suitable for sidebar summaries.</summary>
    public async Task<List<SessionSummary>> LoadAllAsync()
    {
        var index = await ReadIndexAsync();
        return index.Sessions
            .Select(e => new SessionSummary
            {
                Id = e.Id,
                Title = e.Title,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
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

        var content = await _contentStore.ReadAsync(id);
        if (content is null) return null;

        return new SessionRecord
        {
            Id = entry.Id,
            Title = entry.Title,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            Messages = content.Messages,
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
                EnabledTools = session.EnabledTools,
                AgentId = session.AgentId,
            };
            await _contentStore.WriteAsync(session.Id, content);

            var index = await _indexStore.ReadAsync();
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
            await _indexStore.WriteAsync(index);
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

            var index = await _indexStore.ReadAsync();
            index.Sessions.RemoveAll(e => e.Id == id);
            await _indexStore.WriteAsync(index);
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
            var index = await _indexStore.ReadAsync();
            foreach (var entry in index.Sessions)
                TryDeleteSessionDir(entry.Id);

            await _indexStore.WriteAsync(new SessionIndex());
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
    private async Task<SessionIndex> ReadIndexAsync()
    {
        await _lock.WaitAsync();
        try { return await _indexStore.ReadAsync(); }
        finally { _lock.Release(); }
    }

    private void TryDeleteSessionDir(string id)
    {
        try { Directory.Delete(SessionDir(id), recursive: true); }
        catch { }
    }
}
