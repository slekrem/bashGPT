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
        var tools = opts.Tools ?? [];
        var chatSession = new ChatSessionState(
            provider,
            tools,
            systemPrompt: opts.SystemPrompt,
            llmConfig: opts.LlmConfig,
            onReasoningToken: opts.OnReasoningToken,
            onLlmRequestJson: opts.OnLlmRequestJson,
            onLlmResponseJson: opts.OnLlmResponseJson);
        chatSession.InitializeMessages(opts.History, opts.Prompt);

        var commandResults = new List<CommandResult>();
        var usedToolCalls = false;
        var conversationDelta = new List<ChatMessage>();

        try
        {
            var response = await chatSession.CallOnceAsync(opts.OnToken, ct);
            if (response.Error is not null)
                return new ServerChatResult(response.Error, []);

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
                        chatSession.Messages,
                        conversationDelta,
                        toolRegistry,
                        opts.SessionPath,
                        opts.OnEvent,
                        ct));

                    chatSession.RefreshSystemMessages();
                    response = await chatSession.CallOnceAsync(null, ct);
                    if (response.Error is not null) break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var outcome = chatSession.CreateCancelledOutcome();
            var cancelledAssistant = new ChatMessage(ChatRole.Assistant, outcome.Response);
            conversationDelta.Add(cancelledAssistant);

            return new ServerChatResult(
                Response: outcome.Response,
                Logs: logs,
                Usage: outcome.Usage,
                LlmExchanges: outcome.LlmExchanges,
                Commands: commandResults.Count > 0 ? commandResults : null,
                UsedToolCalls: usedToolCalls,
                ConversationDelta: conversationDelta,
                FinalStatus: outcome.FinalStatus);
        }

        var finalStatus = commandResults.Any(r => string.Equals(ClassifyCommandStatus(r), "timeout", StringComparison.Ordinal))
            ? "timeout"
            : "completed";
        var completedOutcome = chatSession.CreateCompletedOutcome(finalStatus);
        var finalAssistantMessage = new ChatMessage(ChatRole.Assistant, completedOutcome.Response);
        conversationDelta.Add(finalAssistantMessage);

        return new ServerChatResult(
            Response: completedOutcome.Response,
            Logs: logs,
            Usage: completedOutcome.Usage,
            LlmExchanges: completedOutcome.LlmExchanges,
            Commands: commandResults.Count > 0 ? commandResults : null,
            UsedToolCalls: usedToolCalls,
            ConversationDelta: conversationDelta,
            FinalStatus: completedOutcome.FinalStatus);
    }

    private static string ClassifyCommandStatus(CommandResult result)
        => ServerToolCallOrchestrator.ClassifyCommandStatus(result);
}
