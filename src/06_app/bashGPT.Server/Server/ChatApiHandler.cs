using System.Net;
using System.Text.Json;
using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;
using bashGPT.Agents;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ChatApiHandler(
    IChatHandler handler,
    ServerToolSelectionPolicy toolSelectionPolicy,
    SessionStore? sessionStore = null,
    SessionRequestStore? sessionRequestStore = null,
    ToolRegistry? toolRegistry = null,
    AgentRegistry? agentRegistry = null)
{
    private readonly ServerSessionService _sessionService = new(sessionStore, sessionRequestStore);

    public async Task HandleAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<ChatRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Prompt is required." }, statusCode: 400);
            return;
        }

        var sessionId = _sessionService.ResolveSessionId(body.SessionId);

        AgentBase? agent = null;
        if (agentRegistry is not null && !string.IsNullOrWhiteSpace(body.AgentId))
            agent = agentRegistry.Find(body.AgentId);

        var session = await _sessionService.LoadAsync(sessionId);
        var historySnapshot = _sessionService.BuildHistorySnapshot(session);

        var effectiveToolNames = agent?.EnabledTools.Count > 0
            ? agent.EnabledTools.ToList()
            : session?.EnabledTools?.Count > 0
                ? session.EnabledTools
                : body.EnabledTools?.ToList();

        var selectableToolNames = agent?.EnabledTools.Count > 0
            ? effectiveToolNames
            : toolSelectionPolicy.FilterRequestedToolNames(effectiveToolNames);

        var resolvedTools = ToolDefinitionMapper.ResolveDefinitions(selectableToolNames, toolRegistry);

        var now = DateTime.UtcNow.ToString("o");
        var requestKey = now + "_" + Guid.NewGuid().ToString("N")[..8];

        var chatOpts = new ServerChatOptions(
            Prompt: body.Prompt.Trim(),
            History: historySnapshot,
            Model: options.Model,
            Verbose: options.Verbose || body.Verbose == true,
            OnLlmRequestJson: sessionId is not null
                ? (idx, json) => _sessionService.SaveLlmRequestAsync(sessionId, requestKey, idx, json)
                : null,
            OnLlmResponseJson: sessionId is not null
                ? (idx, json) => _sessionService.SaveLlmResponseAsync(sessionId, requestKey, idx, json)
                : null,
            Tools: resolvedTools.Count > 0 ? resolvedTools : null,
            SystemPrompt: agent is not null ? sp => agent.GetSystemPrompt(sp) : null,
            SessionPath: _sessionService.GetSessionPath(sessionId));

        var result = await handler.RunServerChatAsync(chatOpts, ct);

        if (sessionId is not null)
            await _sessionService.PersistChatAsync(sessionId, body.Prompt.Trim(), requestKey, now, selectableToolNames, agent?.Id, session, result);

        await ApiResponse.WriteJsonAsync(ctx.Response, new
        {
            response = result.Response,
            usedToolCalls = result.UsedToolCalls,
            finalStatus = result.FinalStatus,
            logs = result.Logs,
            commands = result.Commands,
            usage = result.Usage == null ? null : (object)new
            {
                inputTokens = result.Usage.InputTokens,
                outputTokens = result.Usage.OutputTokens,
                totalTokens = result.Usage.TotalTokens,
                cachedInputTokens = result.Usage.CachedInputTokens,
            },
        });
    }

    private sealed record ChatRequest(string Prompt, bool? Verbose, string? SessionId, string[]? EnabledTools, string? AgentId = null);
}
