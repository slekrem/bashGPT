using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace bashGPT.Core.Chat;

public static class ChatSessionBootstrap
{
    public static async Task<ChatSessionBootstrapResult> CreateAsync(
        ConfigurationService configService,
        string? modelOverride,
        IReadOnlyList<ToolDefinition> tools,
        IEnumerable<ChatMessage> history,
        string prompt,
        Func<AppConfig, string?>? toolChoiceFactory = null,
        Func<IReadOnlyList<string>>? systemPrompt = null,
        AgentLlmConfig? llmConfig = null,
        Action<string>? onReasoningToken = null,
        Func<int, string, Task>? onLlmRequestJson = null,
        Func<int, string, Task>? onLlmResponseJson = null,
        ILlmProvider? providerOverride = null)
    {
        var providerBootstrap = await LlmProviderBootstrap.CreateAsync(configService, modelOverride, providerOverride);
        if (providerBootstrap.Error is not null || providerBootstrap.Provider is null)
        {
            return new ChatSessionBootstrapResult(
                Config: providerBootstrap.Config,
                Provider: null,
                Session: null,
                Error: providerBootstrap.Error ?? "Failed to initialize provider.");
        }

        var toolChoiceName = providerBootstrap.Config is not null && toolChoiceFactory is not null
            ? toolChoiceFactory(providerBootstrap.Config)
            : null;

        var session = ChatSessionFactory.Create(
            providerBootstrap.Provider,
            tools,
            history,
            prompt,
            toolChoiceName,
            systemPrompt,
            llmConfig,
            onReasoningToken,
            onLlmRequestJson,
            onLlmResponseJson);

        return new ChatSessionBootstrapResult(
            providerBootstrap.Config,
            providerBootstrap.Provider,
            session,
            Error: null);
    }
}
