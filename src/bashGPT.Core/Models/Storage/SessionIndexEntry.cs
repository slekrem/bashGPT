namespace BashGPT.Storage;

/// <summary>
/// Compact session metadata entry stored in sessions/index.json.
/// </summary>
public sealed class SessionIndexEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
