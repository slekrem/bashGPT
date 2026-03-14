namespace BashGPT.Agents;

/// <summary>
/// LLM-Konfiguration eines Agenten. Wird im Info-Panel angezeigt und kann
/// zukünftig direkt an den Provider übergeben werden.
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
