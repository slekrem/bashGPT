namespace bashGPT.Core.Models.Providers;

public record LlmChatResponse(
    string Content,
    IReadOnlyList<ToolCall> ToolCalls,
    TokenUsage? Usage = null);
