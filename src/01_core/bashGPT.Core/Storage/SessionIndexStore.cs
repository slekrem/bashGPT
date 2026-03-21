using bashGPT.Core.Models.Storage;

namespace bashGPT.Core.Storage;

internal sealed class SessionIndexStore(string sessionsDir)
{
    private readonly string _indexFile = Path.Combine(sessionsDir, "index.json");

    public async Task<SessionIndex> ReadAsync()
    {
        if (!File.Exists(_indexFile))
            return new SessionIndex();

        return await SessionJsonStorage.ReadAsync<SessionIndex>(_indexFile) ?? new SessionIndex();
    }

    public Task WriteAsync(SessionIndex index) =>
        SessionJsonStorage.WriteAsync(_indexFile, index);
}
