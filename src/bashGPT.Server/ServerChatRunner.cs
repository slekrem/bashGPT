using System.Text.Json;
using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers;
using BashGPT.Configuration;
using BashGPT.Shell;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

public class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null,
    ToolRegistry? toolRegistry = null) : IPromptHandler
{
    public async Task<ServerChatResult> RunServerChatAsync(
        ServerChatOptions opts,
        CancellationToken ct = default)
    {
        var logs = new List<string>();
        var totalInputTokens = 0;
        var totalOutputTokens = 0;

        ILlmProvider provider;
        if (providerOverride is not null)
        {
            provider = providerOverride;
        }
        else
        {
            AppConfig config;
            try
            {
                config = await configService.LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                return new ServerChatResult(
                    Response: $"Configuration error: {ex.Message}",
                    Logs: []);
            }

            ChatOrchestrator.ApplyModelOverride(config, opts.Model);

            try
            {
                provider = ProviderFactory.Create(config);
            }
            catch (Exception ex)
            {
                return new ServerChatResult(
                    Response: $"Provider error: {ex.Message}",
                    Logs: []);
            }
        }

        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, model: {provider.Model}");

        var messages = new List<ChatMessage>();
        void RefreshSystemMessages()
        {
            if (opts.SystemPrompt is null) return;
            var firstNonSystem = messages.FindIndex(m => m.Role != ChatRole.System);
            var removeCount    = firstNonSystem >= 0 ? firstNonSystem : messages.Count;
            if (removeCount > 0) messages.RemoveRange(0, removeCount);
            var freshPrompts = opts.SystemPrompt().Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            for (var i = freshPrompts.Count - 1; i >= 0; i--)
                messages.Insert(0, new ChatMessage(ChatRole.System, freshPrompts[i]));
        }

        RefreshSystemMessages();
        foreach (var msg in opts.History)
            messages.Add(msg);
        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools = opts.Tools ?? [];
        var llmExchanges = new List<LlmExchangeRecord>();
        var exchangeIdx = 0;
        var commandResults = new List<CommandResult>();
        var usedToolCalls = false;
        var conversationDelta = new List<ChatMessage>();
        LlmChatResponse? lastResponse = null;

        async Task<(LlmChatResponse Response, string? Error)> CallOnce(Action<string>? onToken = null)
        {
            var idx = exchangeIdx++;
            string? reqJson = null;
            string? resJson = null;
            var result = await ChatOrchestrator.ChatOnceAsync(
                provider, messages, tools, null, ct, onToken,
                onReasoningToken: opts.OnReasoningToken,
                onRequestJson: async json =>
                {
                    reqJson = json;
                    if (opts.OnLlmRequestJson is not null) await opts.OnLlmRequestJson(idx, json);
                },
                onResponseJson: async json =>
                {
                    resJson = json;
                    if (opts.OnLlmResponseJson is not null) await opts.OnLlmResponseJson(idx, json);
                },
                llmConfig: opts.LlmConfig);
            llmExchanges.Add(new LlmExchangeRecord(reqJson, resJson));
            return result;
        }

        try
        {
            var response = await CallOnce(opts.OnToken);
            if (response.Error is not null)
                return new ServerChatResult(response.Error, []);

            lastResponse = response.Response;
            totalInputTokens += response.Response.Usage?.InputTokens ?? 0;
            totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;

            if (tools.Count > 0 && toolRegistry is not null)
            {
                var round = 0;
                while (!ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    if (response.Response.ToolCalls.Count == 0) break;
                    usedToolCalls = true;

                    round++;
                    opts.OnEvent?.Invoke(new SseEvent("round_start", new { round }));
                    var assistantToolCallMessage = ChatMessage.AssistantWithToolCalls(
                        response.Response.ToolCalls,
                        content: response.Response.Content);
                    messages.Add(assistantToolCallMessage);
                    conversationDelta.Add(assistantToolCallMessage);

                    foreach (var call in response.Response.ToolCalls)
                    {
                        ct.ThrowIfCancellationRequested();

                        var commandLabel = TryExtractCommand(call.ArgumentsJson, out var parsedCommand)
                            ? parsedCommand
                            : call.Name;
                        opts.OnEvent?.Invoke(new SseEvent("tool_call", new { name = call.Name, command = commandLabel }));

                        CommandResult commandResult;
                        string toolResult;
                        if (toolRegistry.TryGet(call.Name, out var iTool) && iTool is not null)
                        {
                            try
                            {
                                var r = await iTool.ExecuteAsync(
                                    new BashGPT.Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson ?? "{}", opts.SessionPath), ct);

                                toolResult = r.Content;
                                commandResult = BuildCommandResult(call.Name, commandLabel, r.Content, r.Success);
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                toolResult = $"Error: {ex.Message}";
                                commandResult = new CommandResult(commandLabel, 1, toolResult, WasExecuted: false);
                            }
                        }
                        else
                        {
                            toolResult = $"Error: Unknown tool '{call.Name}'.";
                            commandResult = new CommandResult(commandLabel, 1, toolResult, WasExecuted: false);
                        }

                        commandResults.Add(commandResult);
                        opts.OnEvent?.Invoke(new SseEvent("command_result", new
                        {
                            command = commandResult.Command,
                            output = commandResult.Output,
                            exitCode = commandResult.ExitCode,
                            wasExecuted = commandResult.WasExecuted,
                            status = ClassifyCommandStatus(commandResult),
                        }));
                        var toolMessage = ChatMessage.ToolResult(toolResult, call.Id, call.Name);
                        messages.Add(toolMessage);
                        conversationDelta.Add(toolMessage);
                    }

                    RefreshSystemMessages();
                    response = await CallOnce();
                    if (response.Error is not null) break;
                    lastResponse = response.Response;
                    totalInputTokens += response.Response.Usage?.InputTokens ?? 0;
                    totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var cancelledText = "Cancelled by user.";
            var cancelledAssistant = new ChatMessage(ChatRole.Assistant, cancelledText);
            conversationDelta.Add(cancelledAssistant);

            TokenUsage? cancelledUsage = totalInputTokens > 0 || totalOutputTokens > 0
                ? new TokenUsage(totalInputTokens, totalOutputTokens)
                : null;

            return new ServerChatResult(
                Response: cancelledText,
                Logs: logs,
                Usage: cancelledUsage,
                LlmExchanges: llmExchanges.Count > 0 ? llmExchanges : null,
                Commands: commandResults.Count > 0 ? commandResults : null,
                UsedToolCalls: usedToolCalls,
                ConversationDelta: conversationDelta,
                FinalStatus: "user_cancelled");
        }

        TokenUsage? BuildUsage() => totalInputTokens > 0 || totalOutputTokens > 0
            ? new TokenUsage(totalInputTokens, totalOutputTokens)
            : null;

        var finalResponse = lastResponse?.Content ?? string.Empty;
        var finalAssistantMessage = new ChatMessage(ChatRole.Assistant, finalResponse);
        conversationDelta.Add(finalAssistantMessage);

        var finalStatus = commandResults.Any(r => string.Equals(ClassifyCommandStatus(r), "timeout", StringComparison.Ordinal))
            ? "timeout"
            : "completed";

        return new ServerChatResult(
            Response: finalResponse,
            Logs: logs,
            Usage: BuildUsage(),
            LlmExchanges: llmExchanges.Count > 0 ? llmExchanges : null,
            Commands: commandResults.Count > 0 ? commandResults : null,
            UsedToolCalls: usedToolCalls,
            ConversationDelta: conversationDelta,
            FinalStatus: finalStatus);
    }

    private static bool TryExtractCommand(string? argumentsJson, out string command)
    {
        command = string.Empty;
        if (string.IsNullOrWhiteSpace(argumentsJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (!doc.RootElement.TryGetProperty("command", out var commandEl)) return false;
            var value = commandEl.GetString();
            if (string.IsNullOrWhiteSpace(value)) return false;
            command = value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static CommandResult BuildCommandResult(string toolName, string commandLabel, string content, bool success)
    {
        var exitCode = success ? 0 : 1;
        return new CommandResult($"{toolName}: {commandLabel}", exitCode, content, WasExecuted: success);
    }

    private static string ClassifyCommandStatus(CommandResult result)
        => result.Output.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            ? "timeout"
            : result.WasExecuted
                ? "executed"
                : "failed";
}
