namespace bashGPT.Core.Models.Storage;

public sealed class SessionToolCall
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}
