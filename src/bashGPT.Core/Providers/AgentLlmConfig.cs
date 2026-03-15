namespace BashGPT.Providers;

/// <summary>
/// LLM-Konfiguration eines Agenten. Wird direkt an den Provider übergeben.
/// </summary>
public record AgentLlmConfig(
    string?  Model              = null,
    double?  Temperature        = null,
    double?  TopP               = null,
    int?     NumCtx             = null,
    int?     MaxTokens          = null,
    int?     Seed               = null,
    string?  ReasoningEffort    = null,
    bool?    ParallelToolCalls  = null,
    bool     Stream             = true
);
