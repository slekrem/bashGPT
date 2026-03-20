using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace bashGPT.Core.Chat;

public static class ChatSessionFactory
{
    public static ChatSessionState Create(
        ILlmProvider provider,
        IReadOnlyList<ProviderToolDefinition> tools,
        IEnumerable<ChatMessage> history,
        string prompt,
        string? toolChoiceName = null,
        Func<IReadOnlyList<string>>? systemPrompt = null,
        AgentLlmConfig? llmConfig = null,
        Action<string>? onReasoningToken = null,
        Func<int, string, Task>? onLlmRequestJson = null,
        Func<int, string, Task>? onLlmResponseJson = null)
    {
        var session = new ChatSessionState(
            provider,
            tools,
            toolChoiceName,
            systemPrompt,
            llmConfig,
            onReasoningToken,
            onLlmRequestJson,
            onLlmResponseJson);

        session.InitializeMessages(history, prompt);
        return session;
    }
}
