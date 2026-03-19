namespace bashGPT.Core.Providers;

public record LlmChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ToolDefinition>? Tools = null,
    string? ToolChoiceName = null,
    bool? ParallelToolCalls = null,
    bool Stream = true,
    Action<string>? OnToken = null,
    Action<string>? OnReasoningToken = null,
    Func<string, Task>? OnRequestJson = null,
    Func<string, Task>? OnResponseJson = null,
    double? Temperature = null,
    double? TopP = null,
    int? NumCtx = null,
    int? MaxTokens = null,
    int? Seed = null,
    string? ReasoningEffort = null,
    double? FrequencyPenalty = null,
    double? PresencePenalty = null,
    IReadOnlyList<string>? Stop = null,
    string? ResponseFormat = null);
