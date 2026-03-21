using bashGPT.Core.Models.Storage;

namespace bashGPT.Core.Storage;

public sealed class SessionRequestStore
{
    private readonly string _sessionsDir;

    public SessionRequestStore(string sessionsDir)
    {
        _sessionsDir = sessionsDir;
    }

    public async Task SaveRequestAsync(string sessionId, SessionRequestRecord record)
    {
        var path = GetRequestRecordFilePath(sessionId, record.Timestamp);
        await SessionJsonStorage.WriteAsync(path, record);
    }

    public async Task SaveLlmRequestAsync(string sessionId, string timestamp, string llmRequestJson)
    {
        var path = GetLlmRequestFilePath(sessionId, timestamp);
        await SessionJsonStorage.WriteRawAsync(path, llmRequestJson);
    }

    public async Task SaveLlmResponseAsync(string sessionId, string timestamp, string llmResponseJson)
    {
        var path = GetLlmResponseFilePath(sessionId, timestamp);
        await SessionJsonStorage.WriteRawAsync(path, llmResponseJson);
    }

    private string GetRequestRecordFilePath(string sessionId, string timestamp)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);
        return SessionStoragePaths.GetRequestRecordFilePath(_sessionsDir, sessionId, timestamp);
    }

    private string GetLlmRequestFilePath(string sessionId, string timestamp)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);
        return SessionStoragePaths.GetLlmRequestFilePath(_sessionsDir, sessionId, timestamp);
    }

    private string GetLlmResponseFilePath(string sessionId, string timestamp)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);
        return SessionStoragePaths.GetLlmResponseFilePath(_sessionsDir, sessionId, timestamp);
    }
}
