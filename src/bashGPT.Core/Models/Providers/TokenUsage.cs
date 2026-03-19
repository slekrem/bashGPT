namespace bashGPT.Core.Models.Providers;

public record TokenUsage(
    int InputTokens,
    int OutputTokens,
    int? TotalTokens = null,
    int? CachedInputTokens = null);
