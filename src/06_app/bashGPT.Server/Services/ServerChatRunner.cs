using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Configuration;
using bashGPT.Tools.Registration;
using bashGPT.Server.Models;

namespace bashGPT.Server.Services;

public sealed class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null,
    ToolRegistry? toolRegistry = null) : IChatHandler
{
    public async Task<ServerChatResult> RunServerChatAsync(
        ServerChatOptions opts,
        CancellationToken ct = default)
    {
        var logs = new List<string>();

        var tools = opts.Tools ?? [];
        var sessionPath = opts.SessionPath;
        var bootstrap = await ChatSessionBootstrap.CreateAsync(
            configService,
            opts.Model,
            tools,
            opts.History,
            opts.Prompt,
            systemPrompt: opts.SystemPrompt is not null ? () => opts.SystemPrompt(sessionPath) : null,
            llmConfig: opts.LlmConfig,
            onReasoningToken: opts.OnReasoningToken,
            onLlmRequestJson: opts.OnLlmRequestJson,
            onLlmResponseJson: opts.OnLlmResponseJson,
            providerOverride: providerOverride);

        if (bootstrap.Error is not null || bootstrap.Provider is null || bootstrap.Session is null)
        {
            return new ServerChatResult(
                Response: bootstrap.Error ?? "Failed to initialize provider.",
                Logs: []);
        }

        var provider = bootstrap.Provider;
        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, model: {provider.Model}");
        var chatSession = bootstrap.Session;

        var commandResults = new List<SessionCommand>();
        var usedToolCalls = false;
        var conversationDelta = new List<ChatMessage>();

        try
        {
            var runResult = await ChatSessionRunner.RunAsync(
                chatSession,
                opts.OnToken,
                enableToolCalls: tools.Count > 0 && toolRegistry is not null,
                async (round, currentResponse) =>
                {
                    opts.OnEvent?.Invoke(new SseEvent("round_start", new { round }));
                    commandResults.AddRange(await ServerToolCallOrchestrator.ExecuteRoundAsync(
                        currentResponse.ToolCalls,
                        currentResponse.Content,
                        chatSession.Messages,
                        conversationDelta,
                        toolRegistry!,
                        opts.SessionPath,
                        opts.OnEvent,
                        ct,
                        opts.Agent));
                },
                beforeNextCall: chatSession.RefreshSystemMessages,
                ct);

            if (runResult.Error is not null)
                return new ServerChatResult(runResult.Error, []);

            usedToolCalls = runResult.UsedToolCalls;
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

        var finalStatus = commandResults.Any(r => string.Equals(ServerToolCallOrchestrator.ClassifyCommandStatus(r), "timeout", StringComparison.Ordinal))
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
}
