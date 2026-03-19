namespace bashGPT.Core.Providers;

public record LlmChatResponse(
    string Content,
    IReadOnlyList<ToolCall> ToolCalls,
    TokenUsage? Usage = null);
