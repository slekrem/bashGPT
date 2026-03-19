namespace bashGPT.Core.Providers;

/// <summary>
/// LLM-Konfiguration eines Agenten. Wird direkt an den Provider übergeben.
/// Unterstützte Felder orientieren sich an Ollama /v1/chat/completions.
/// </summary>
public record AgentLlmConfig(
    string?                   Model              = null,
    double?                   Temperature        = null,
    double?                   TopP               = null,
    int?                      NumCtx             = null,
    int?                      MaxTokens          = null,
    int?                      Seed               = null,
    string?                   ReasoningEffort    = null,
    bool?                     ParallelToolCalls  = null,
    bool                      Stream             = true,
    double?                   FrequencyPenalty   = null,
    double?                   PresencePenalty    = null,
    IReadOnlyList<string>?    Stop               = null,
    string?                   ResponseFormat     = null
);
