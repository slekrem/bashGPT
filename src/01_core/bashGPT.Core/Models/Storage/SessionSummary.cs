namespace bashGPT.Core.Models.Storage;

/// <summary>
/// Represents session metadata for lists and sidebar summaries.
/// </summary>
public sealed class SessionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
