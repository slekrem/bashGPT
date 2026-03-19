namespace bashGPT.Core.Models.Storage;

public sealed class SessionResponseData
{
    public string Content { get; set; } = string.Empty;
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage? Usage { get; set; }
}
