using System.Text.Json;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;
using bashGPT.Agents;
using bashGPT.Tools.Registration;
using Microsoft.AspNetCore.Http.Features;

namespace bashGPT.Server;

internal sealed class StreamingChatApiHandler(
    IChatHandler handler,
    RunningChatRegistry runningChats,
    SessionStore? sessionStore = null,
    SessionRequestStore? sessionRequestStore = null,
    ToolRegistry? toolRegistry = null,
    AgentRegistry? agentRegistry = null)
{
    private readonly ServerSessionService _sessionService = new(sessionStore, sessionRequestStore);

    public async Task HandleAsync(HttpContext ctx, ServerOptions options, CancellationToken ct)
    {
        var body = await ctx.Request.ReadFromJsonAsync<ChatRequest>(JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            await ctx.Response.WriteJsonAsync(new { error = "Prompt is required." }, statusCode: 400);
            return;
        }

        var requestId = string.IsNullOrWhiteSpace(body.RequestId)
            ? Guid.NewGuid().ToString("N")
            : body.RequestId.Trim();
        var sessionId = _sessionService.ResolveSessionId(body.SessionId);

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runningChats.Register(requestId, requestCts);

        ctx.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "text/event-stream; charset=utf-8";
        ctx.Response.Headers["Cache-Control"] = "no-cache";
        ctx.Response.Headers["X-Accel-Buffering"] = "no";

        var stream = ctx.Response.Body;
        var sse = new StreamingSseWriter(stream);

        try
        {
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

            var selectableToolNames = effectiveToolNames;

            var resolvedTools = ToolDefinitionMapper.ResolveDefinitions(selectableToolNames, toolRegistry, agent);

            var now = DateTime.UtcNow.ToString("o");
            var requestKey = now + "_" + Guid.NewGuid().ToString("N")[..8];

            var chatOpts = new ServerChatOptions(
                Prompt: body.Prompt.Trim(),
                History: historySnapshot,
                Model: options.Model,
                Verbose: options.Verbose || body.Verbose == true,
                OnToken: sse.WriteContentToken,
                OnReasoningToken: sse.WriteReasoningToken,
                OnEvent: sse.WriteEvent,
                OnLlmRequestJson: sessionId is not null
                    ? (idx, json) => _sessionService.SaveLlmRequestAsync(sessionId, requestKey, idx, json)
                    : null,
                OnLlmResponseJson: sessionId is not null
                    ? (idx, json) => _sessionService.SaveLlmResponseAsync(sessionId, requestKey, idx, json)
                    : null,
                Tools: resolvedTools.Count > 0 ? resolvedTools : null,
                SystemPrompt: agent is not null ? sp => agent.GetSystemPrompt(sp) : null,
                LlmConfig: agent?.LlmConfig,
                SessionPath: _sessionService.GetSessionPath(sessionId),
                Agent: agent);

            var result = await handler.RunServerChatAsync(chatOpts, requestCts.Token);

            sse.WriteDone(result, requestId);

            if (sessionId is not null)
                await _sessionService.PersistChatAsync(sessionId, body.Prompt.Trim(), requestKey, now, selectableToolNames, agent?.Id, session, result);
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try
            {
                sse.WriteCancelled(requestId);
            }
            catch { }
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"[server] Streaming request failed: {ex}");
                sse.WriteError(ApiErrors.GenericServerError);
            }
            catch { }
        }
        finally
        {
            runningChats.Unregister(requestId);
        }
    }

    private sealed record ChatRequest(
        string Prompt,
        bool? Verbose,
        string? SessionId,
        string[]? EnabledTools,
        string? AgentId = null,
        string? RequestId = null);
}
