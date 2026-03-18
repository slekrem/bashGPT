namespace bashGPT.Core.Models.Storage;

/// <summary>
/// Represents a single chat session with metadata and messages.
/// </summary>
public sealed class SessionRecord
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<SessionMessage> Messages { get; set; } = [];
    public SessionShellContext? ShellContext { get; set; }
    public List<string>? EnabledTools { get; set; }
    public string? AgentId { get; set; }
}
