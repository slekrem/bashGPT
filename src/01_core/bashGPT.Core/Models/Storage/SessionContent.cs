namespace bashGPT.Core.Models.Storage;

/// <summary>
/// Content document stored in sessions/&lt;id&gt;/content.json.
/// </summary>
public sealed class SessionContent
{
    public List<SessionMessage> Messages { get; set; } = [];
    public List<string>? EnabledTools { get; set; }
    public string? AgentId { get; set; }
}
