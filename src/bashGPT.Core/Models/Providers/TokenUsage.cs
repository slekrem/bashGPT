namespace bashGPT.Core.Providers;

public record TokenUsage(
    int InputTokens,
    int OutputTokens,
    int? TotalTokens = null,
    int? CachedInputTokens = null);
