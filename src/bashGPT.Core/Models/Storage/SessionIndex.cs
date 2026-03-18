namespace BashGPT.Storage;

/// <summary>
/// Root document stored in sessions/index.json.
/// </summary>
public sealed class SessionIndex
{
    public int Version { get; set; } = 1;
    public List<SessionIndexEntry> Sessions { get; set; } = [];
}
