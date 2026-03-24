using bashGPT.Agents;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Server.Models;
using bashGPT.Server.Services;
using bashGPT.Tools.Registration;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(
    IChatHandler handler,
    RunningChatRegistry runningChats,
    ServerSessionService sessionService,
    ToolRegistry? toolRegistry = null,
    AgentRegistry? agentRegistry = null) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body?.Prompt))
            return BadRequest(new { error = "Prompt is required." });

        var sessionId = sessionService.ResolveSessionId(body.SessionId);
        var (agent, session, resolvedTools, effectiveToolNames) = await ResolveContextAsync(body.AgentId, body.EnabledTools, sessionId);
        var (requestKey, now) = NewRequestKey();

        var chatOpts = BuildChatOptions(body.Prompt.Trim(), body.Verbose, sessionId, requestKey,
            session, resolvedTools, agent, onToken: null, onReasoning: null, onEvent: null);

        var result = await handler.RunServerChatAsync(chatOpts, ct);

        if (sessionId is not null)
            await sessionService.PersistChatAsync(sessionId, body.Prompt.Trim(), requestKey, now, effectiveToolNames, agent?.Id, session, result);

        return Ok(new
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

    [HttpPost("cancel")]
    public IActionResult Cancel([FromBody] CancelRequest body)
    {
        if (string.IsNullOrWhiteSpace(body?.RequestId))
            return BadRequest(new { error = "requestId is required." });

        var cancelled = runningChats.Cancel(body.RequestId.Trim());
        return Ok(new { ok = true, cancelled, requestId = body.RequestId.Trim() });
    }

    [HttpPost("stream")]
    public async Task Stream([FromBody] StreamChatRequest body, CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Prompt is required." });
            return;
        }

        var requestId = string.IsNullOrWhiteSpace(body.RequestId)
            ? Guid.NewGuid().ToString("N")
            : body.RequestId.Trim();
        var sessionId = sessionService.ResolveSessionId(body.SessionId);

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runningChats.Register(requestId, requestCts);

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        HttpContext.Response.StatusCode = 200;
        HttpContext.Response.ContentType = "text/event-stream; charset=utf-8";
        HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var sse = new StreamingSseWriter(HttpContext.Response.Body);

        try
        {
            var (agent, session, resolvedTools, effectiveToolNames) =
                await ResolveContextAsync(body.AgentId, body.EnabledTools, sessionId);
            var (requestKey, now) = NewRequestKey();

            var chatOpts = BuildChatOptions(body.Prompt.Trim(), body.Verbose, sessionId, requestKey,
                session, resolvedTools, agent,
                onToken: sse.WriteContentToken,
                onReasoning: sse.WriteReasoningToken,
                onEvent: sse.WriteEvent,
                llmConfig: agent?.LlmConfig);

            var result = await handler.RunServerChatAsync(chatOpts, requestCts.Token);
            sse.WriteDone(result, requestId);

            if (sessionId is not null)
                await sessionService.PersistChatAsync(sessionId, body.Prompt.Trim(), requestKey, now, effectiveToolNames, agent?.Id, session, result);
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try { sse.WriteCancelled(requestId); } catch { }
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

    private static (string Key, string Timestamp) NewRequestKey()
    {
        var now = DateTime.UtcNow.ToString("o");
        return (now + "_" + Guid.NewGuid().ToString("N")[..8], now);
    }

    private async Task<(AgentBase? agent, SessionRecord? session, IReadOnlyList<ProviderToolDefinition> resolvedTools, List<string>? effectiveToolNames)>
        ResolveContextAsync(string? agentId, string[]? requestTools, string? sessionId)
    {
        AgentBase? agent = null;
        if (agentRegistry is not null && !string.IsNullOrWhiteSpace(agentId))
            agent = agentRegistry.Find(agentId);

        var session = await sessionService.LoadAsync(sessionId);

        var effectiveToolNames = agent?.EnabledTools.Count > 0
            ? [.. agent.EnabledTools]
            : session?.EnabledTools?.Count > 0
                ? session.EnabledTools
                : requestTools?.ToList();

        var resolvedTools = ToolDefinitionMapper.ResolveDefinitions(effectiveToolNames, toolRegistry, agent);
        return (agent, session, resolvedTools, effectiveToolNames);
    }

    private ServerChatOptions BuildChatOptions(
        string prompt, bool? verbose, string? sessionId, string requestKey,
        SessionRecord? session, IReadOnlyList<ProviderToolDefinition> resolvedTools, AgentBase? agent,
        Action<string>? onToken, Action<string>? onReasoning, Action<SseEvent>? onEvent,
        AgentLlmConfig? llmConfig = null)
    {
        var historySnapshot = sessionService.BuildHistorySnapshot(session);
        return new ServerChatOptions(
            Prompt: prompt,
            History: historySnapshot,
            Model: null,
            Verbose: verbose == true,
            OnToken: onToken,
            OnReasoningToken: onReasoning,
            OnEvent: onEvent,
            OnLlmRequestJson: sessionId is not null
                ? (idx, json) => sessionService.SaveLlmRequestAsync(sessionId, requestKey, idx, json)
                : null,
            OnLlmResponseJson: sessionId is not null
                ? (idx, json) => sessionService.SaveLlmResponseAsync(sessionId, requestKey, idx, json)
                : null,
            Tools: resolvedTools.Count > 0 ? resolvedTools : null,
            SystemPrompt: agent is not null ? sp => agent.GetSystemPrompt(sp) : null,
            LlmConfig: llmConfig,
            SessionPath: sessionService.GetSessionPath(sessionId),
            Agent: agent);
    }
}
