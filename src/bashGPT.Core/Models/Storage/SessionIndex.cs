namespace bashGPT.Core.Models.Storage;

/// <summary>
/// Root document stored in sessions/index.json.
/// </summary>
public sealed class SessionIndex
{
    public List<SessionIndexEntry> Sessions { get; set; } = [];
}
