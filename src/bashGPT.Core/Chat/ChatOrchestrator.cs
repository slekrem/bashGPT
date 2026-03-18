using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace bashGPT.Core.Chat;

public sealed record ParsedToolCommand(ToolCall ToolCall, ExtractedCommand Command);
public sealed record ToolCallError(ToolCall ToolCall, string Error);

/// <summary>
/// Shared chat orchestration helpers for provider calls, tool-call parsing,
/// and tool execution without any UI-specific dependencies.
/// </summary>
public static class ChatOrchestrator
{
    public static void ApplyModelOverride(
        AppConfig config,
        ProviderType? providerOverride,
        string? modelOverride)
    {
        if (modelOverride is null) return;
        _ = providerOverride;
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
                    Temperature:      llmConfig?.Temperature,
                    TopP:             llmConfig?.TopP,
                    NumCtx:           llmConfig?.NumCtx,
                    MaxTokens:        llmConfig?.MaxTokens,
                    Seed:             llmConfig?.Seed,
                    ReasoningEffort:  llmConfig?.ReasoningEffort,
                    FrequencyPenalty: llmConfig?.FrequencyPenalty,
                    PresencePenalty:  llmConfig?.PresencePenalty,
                    Stop:             llmConfig?.Stop,
                    ResponseFormat:   llmConfig?.ResponseFormat),
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

    public static (List<ParsedToolCommand> Commands, List<ToolCallError> Errors) ParseToolCalls(
        IReadOnlyList<ToolCall> toolCalls)
    {
        var commands = new List<ParsedToolCommand>();
        var errors   = new List<ToolCallError>();

        foreach (var call in toolCalls)
        {
            if (!ToolCallParsing.TryGetCommand(call, out var command, out var error))
            {
                errors.Add(new ToolCallError(call, error ?? "Unknown error."));
                continue;
            }

            var (isDangerous, reason) = BashCommandExtractor.CheckDanger(command);
            commands.Add(new ParsedToolCommand(call, new ExtractedCommand(command, isDangerous, reason)));
        }

        return (commands, errors);
    }

    public static async Task<IReadOnlyList<CommandResult>> ExecuteToolCallRoundAsync(
        IReadOnlyList<ToolCall> toolCalls,
        IReadOnlyList<ParsedToolCommand> commands,
        IReadOnlyList<ToolCallError> errors,
        string currentContent,
        List<ChatMessage> messages,
        CommandExecutor executor,
        CancellationToken ct)
    {
        var results = commands.Count > 0
            ? await executor.ProcessAsync(commands.Select(c => c.Command).ToList(), ct)
            : [];

        messages.Add(ChatMessage.AssistantWithToolCalls(toolCalls, currentContent));

        foreach (var msg in BuildToolResultMessages(toolCalls, commands, results, errors))
            messages.Add(msg);

        return results;
    }

    private static IReadOnlyList<ChatMessage> BuildToolResultMessages(
        IReadOnlyList<ToolCall> toolCalls,
        IReadOnlyList<ParsedToolCommand> commands,
        IReadOnlyList<CommandResult> results,
        IReadOnlyList<ToolCallError> errors)
    {
        var messages       = new List<ChatMessage>();
        var commandResults = results.ToList();

        for (var i = 0; i < commands.Count; i++)
        {
            var call   = commands[i].ToolCall;
            var result = i < commandResults.Count
                ? commandResults[i]
                : new CommandResult(commands[i].Command.Command, -1, "Not executed.", false);

            messages.Add(ChatMessage.ToolResult(
                FormatToolResult(result),
                toolCallId: call.Id,
                toolName:   call.Name));
        }

        foreach (var err in errors)
        {
            messages.Add(ChatMessage.ToolResult(
                $"Error: {err.Error}",
                toolCallId: err.ToolCall.Id,
                toolName:   err.ToolCall.Name));
        }

        return messages;
    }

    private static string FormatToolResult(CommandResult result)
    {
        var output = string.IsNullOrWhiteSpace(result.Output)
            ? "(no output)"
            : result.Output;

        var status = result.WasExecuted
            ? $"Exit code: {result.ExitCode}"
            : "Not executed";

        return $"{status}\n{output}";
    }
}
