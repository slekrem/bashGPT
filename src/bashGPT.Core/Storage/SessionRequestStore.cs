using System.Text.Json;
using System.Text.Json.Serialization;
using bashGPT.Core.Models.Storage;

namespace bashGPT.Core.Storage;

public sealed class SessionRequestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

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
        var json = JsonSerializer.Serialize(record, JsonOptions);

        await WriteAtomicAsync(path, json);
    }

    public async Task SaveLlmRequestAsync(string sessionId, string timestamp, string llmRequestJson)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);

        var dir = SessionStoragePaths.GetRequestsDir(_sessionsDir, sessionId);
        Directory.CreateDirectory(dir);

        var safeName = SessionStoragePaths.GetSafeTimestamp(timestamp);
        var path = Path.Combine(dir, safeName + "-llm-request.json");

        await WriteAtomicAsync(path, llmRequestJson);
    }

    public async Task SaveLlmResponseAsync(string sessionId, string timestamp, string llmResponseJson)
    {
        SessionStoragePaths.ValidateSessionId(_sessionsDir, sessionId);

        var dir = SessionStoragePaths.GetRequestsDir(_sessionsDir, sessionId);
        Directory.CreateDirectory(dir);

        var safeName = SessionStoragePaths.GetSafeTimestamp(timestamp);
        var path = Path.Combine(dir, safeName + "-llm-response.json");

        await WriteAtomicAsync(path, llmResponseJson);
    }

    private static async Task WriteAtomicAsync(string path, string content)
    {
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }
}
