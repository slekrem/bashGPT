using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Configuration;
using BashGPT.Shell;
using BashGPT.Tools.Execution;

namespace bashGPT.Server;

public class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null,
    ToolRegistry? toolRegistry = null) : IChatHandler
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
            var bootstrap = await LlmProviderBootstrap.CreateAsync(configService, opts.Model);
            if (bootstrap.Error is not null || bootstrap.Provider is null)
            {
                return new ServerChatResult(
                    Response: bootstrap.Error ?? "Failed to initialize provider.",
                    Logs: []);
            }

            provider = bootstrap.Provider;
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
                    commandResults.AddRange(await ServerToolCallOrchestrator.ExecuteRoundAsync(
                        response.Response.ToolCalls,
                        response.Response.Content,
                        messages,
                        conversationDelta,
                        toolRegistry,
                        opts.SessionPath,
                        opts.OnEvent,
                        ct));

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

    private static string ClassifyCommandStatus(CommandResult result)
        => ServerToolCallOrchestrator.ClassifyCommandStatus(result);
}
