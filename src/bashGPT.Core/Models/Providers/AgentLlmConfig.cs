namespace bashGPT.Core.Providers;

/// <summary>
/// Agent-specific LLM configuration passed through to the provider layer.
/// Supported fields follow the Ollama /v1/chat/completions payload.
/// </summary>
public record AgentLlmConfig(
    string? Model = null,
    double? Temperature = null,
    double? TopP = null,
    int? NumCtx = null,
    int? MaxTokens = null,
    int? Seed = null,
    string? ReasoningEffort = null,
    bool? ParallelToolCalls = null,
    bool Stream = true,
    double? FrequencyPenalty = null,
    double? PresencePenalty = null,
    IReadOnlyList<string>? Stop = null,
    string? ResponseFormat = null
);
