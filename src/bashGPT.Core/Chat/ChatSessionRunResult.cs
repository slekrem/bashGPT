using bashGPT.Core.Models.Providers;

namespace bashGPT.Core.Chat;

public sealed record ChatSessionRunResult(
    LlmChatResponse Response,
    string? Error,
    bool UsedToolCalls,
    int ToolCallRounds);
