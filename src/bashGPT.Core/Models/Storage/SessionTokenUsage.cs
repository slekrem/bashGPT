namespace BashGPT.Storage;

public sealed class SessionTokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int? CachedInputTokens { get; set; }
}
