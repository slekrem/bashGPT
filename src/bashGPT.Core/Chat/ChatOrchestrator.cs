using bashGPT.Core.Providers;
using BashGPT.Configuration;

namespace bashGPT.Core.Chat;

/// <summary>
/// Shared chat orchestration helpers for provider calls and model overrides.
/// </summary>
public static class ChatOrchestrator
{
    public static void ApplyModelOverride(
        AppConfig config,
        string? modelOverride)
    {
        if (modelOverride is null) return;
        config.Ollama.Model = modelOverride;
    }

    public static async Task<(LlmChatResponse Response, string? Error)> ChatOnceAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? toolChoiceName,
        CancellationToken ct,
        Action<string>? onToken = null,
        Action<string>? onReasoningToken = null,
        Func<string, Task>? onRequestJson = null,
        Func<string, Task>? onResponseJson = null,
        AgentLlmConfig? llmConfig = null)
    {
        try
        {
            var tokenBuffer = new System.Text.StringBuilder();
            var response = await provider.ChatAsync(
                new LlmChatRequest(
                    Messages: messages,
                    Tools: tools,
                    ToolChoiceName: toolChoiceName,
                    ParallelToolCalls: llmConfig?.ParallelToolCalls ?? false,
                    Stream: llmConfig?.Stream ?? true,
                    OnToken: token => { tokenBuffer.Append(token); onToken?.Invoke(token); },
                    OnReasoningToken: onReasoningToken,
                    OnRequestJson: onRequestJson,
                    OnResponseJson: onResponseJson,
                    Temperature: llmConfig?.Temperature,
                    TopP: llmConfig?.TopP,
                    NumCtx: llmConfig?.NumCtx,
                    MaxTokens: llmConfig?.MaxTokens,
                    Seed: llmConfig?.Seed,
                    ReasoningEffort: llmConfig?.ReasoningEffort,
                    FrequencyPenalty: llmConfig?.FrequencyPenalty,
                    PresencePenalty: llmConfig?.PresencePenalty,
                    Stop: llmConfig?.Stop,
                    ResponseFormat: llmConfig?.ResponseFormat),
                ct);

            if (string.IsNullOrWhiteSpace(response.Content) && tokenBuffer.Length > 0)
                response = response with { Content = tokenBuffer.ToString() };

            return (response, null);
        }
        catch (LlmProviderException ex)
        {
            return (new LlmChatResponse("", []), $"Error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return (new LlmChatResponse("", []), "Cancelled.");
        }
    }
}
