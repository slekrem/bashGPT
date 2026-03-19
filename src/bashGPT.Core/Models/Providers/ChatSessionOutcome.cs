namespace bashGPT.Core.Models.Providers;

public record ChatSessionOutcome(
    string Response,
    TokenUsage? Usage,
    IReadOnlyList<LlmExchangeRecord>? LlmExchanges,
    string FinalStatus);
