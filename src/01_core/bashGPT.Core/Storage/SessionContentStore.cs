using bashGPT.Core.Models.Storage;

namespace bashGPT.Core.Storage;

internal sealed class SessionContentStore(string sessionsDir)
{
    public Task<SessionContent?> ReadAsync(string sessionId)
    {
        var path = SessionStoragePaths.GetContentFilePath(sessionsDir, sessionId);
        return SessionJsonStorage.ReadAsync<SessionContent>(path);
    }

    public Task WriteAsync(string sessionId, SessionContent content)
    {
        var path = SessionStoragePaths.GetContentFilePath(sessionsDir, sessionId);
        return SessionJsonStorage.WriteAsync(path, content);
    }
}
