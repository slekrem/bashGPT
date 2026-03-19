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
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);

        var dir = SessionStoragePaths.GetRequestsDir(_sessionsDir, sessionId);
        Directory.CreateDirectory(dir);

        var safeName = SessionStoragePaths.GetSafeTimestamp(record.Timestamp);
        var path = Path.Combine(dir, safeName + ".json");
        await SessionJsonStorage.WriteAsync(path, record);
    }

    public async Task SaveLlmRequestAsync(string sessionId, string timestamp, string llmRequestJson)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);

        var dir = SessionStoragePaths.GetRequestsDir(_sessionsDir, sessionId);
        Directory.CreateDirectory(dir);

        var safeName = SessionStoragePaths.GetSafeTimestamp(timestamp);
        var path = Path.Combine(dir, safeName + "-llm-request.json");

        await SessionJsonStorage.WriteRawAsync(path, llmRequestJson);
    }

    public async Task SaveLlmResponseAsync(string sessionId, string timestamp, string llmResponseJson)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);

        var dir = SessionStoragePaths.GetRequestsDir(_sessionsDir, sessionId);
        Directory.CreateDirectory(dir);

        var safeName = SessionStoragePaths.GetSafeTimestamp(timestamp);
        var path = Path.Combine(dir, safeName + "-llm-response.json");

        await SessionJsonStorage.WriteRawAsync(path, llmResponseJson);
    }
}
