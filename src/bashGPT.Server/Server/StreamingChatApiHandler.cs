using System.Net;
using System.Text.Json;
using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Shell;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

internal sealed class StreamingChatApiHandler(
    IPromptHandler handler,
    RunningChatRegistry runningChats,
    ServerToolSelectionPolicy toolSelectionPolicy,
    SessionStore? sessionStore = null,
    SessionRequestStore? sessionRequestStore = null,
    ToolRegistry? toolRegistry = null,
    AgentRegistry? agentRegistry = null)
{
    public async Task HandleAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<ChatRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Prompt is required." }, statusCode: 400);
            return;
        }

        var requestId = string.IsNullOrWhiteSpace(body.RequestId)
            ? Guid.NewGuid().ToString("N")
            : body.RequestId.Trim();
        var sessionId = ResolveSessionId(body.SessionId);

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runningChats.Register(requestId, requestCts);

        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "text/event-stream; charset=utf-8";
        ctx.Response.Headers["Cache-Control"] = "no-cache";
        ctx.Response.Headers["X-Accel-Buffering"] = "no";

        var stream = ctx.Response.OutputStream;

        try
        {
            AgentBase? agent = null;
            if (agentRegistry is not null && !string.IsNullOrWhiteSpace(body.AgentId))
                agent = agentRegistry.Find(body.AgentId);

            SessionRecord? session = null;
            if (sessionStore is not null && sessionId is not null)
                session = await sessionStore.LoadAsync(sessionId);

            IReadOnlyList<ChatMessage> historySnapshot;
            if (session is not null)
            {
                historySnapshot = session.Messages
                    .Select(SessionMessageMapper.ToChatMessage)
                    .OfType<ChatMessage>()
                    .ToList();
            }
            else if (sessionStore is not null && sessionId is not null)
            {
                historySnapshot = [];
            }
            else
            {
                historySnapshot = [];
            }

            var effectiveToolNames = agent?.EnabledTools.Count > 0
                ? agent.EnabledTools.ToList()
                : session?.EnabledTools?.Count > 0
                    ? session.EnabledTools
                    : body.EnabledTools?.ToList();

            var selectableToolNames = agent?.EnabledTools.Count > 0
                ? effectiveToolNames
                : toolSelectionPolicy.FilterRequestedToolNames(effectiveToolNames);

            var resolvedTools = ToolHelper.Resolve(selectableToolNames, toolRegistry);

            var now = DateTime.UtcNow.ToString("o");
            var requestKey = now + "_" + Guid.NewGuid().ToString("N")[..8];

            ContextFileCache.CurrentSessionPath = sessionStore is not null && sessionId is not null
                ? sessionStore.GetSessionDir(sessionId)
                : null;

            var chatOpts = new ServerChatOptions(
                Prompt: body.Prompt.Trim(),
                History: historySnapshot,
                Model: options.Model,
                Verbose: options.Verbose || body.Verbose == true,
                OnToken: token =>
                {
                    var json = JsonSerializer.Serialize(
                        new { choices = new[] { new { delta = new { content = token } } } },
                        JsonDefaults.Options);
                    ApiResponse.WriteSseEvent(stream, json);
                },
                OnReasoningToken: token =>
                {
                    var json = JsonSerializer.Serialize(
                        new { choices = new[] { new { delta = new { reasoning = token } } } },
                        JsonDefaults.Options);
                    ApiResponse.WriteSseEvent(stream, json);
                },
                OnEvent: evt =>
                {
                    var json = JsonSerializer.Serialize(
                        new { choices = new[] { new { delta = new { content = "", bashgpt = new { @event = evt.Event, data = evt.Data } } } } },
                        JsonDefaults.Options);
                    ApiResponse.WriteSseEvent(stream, json);
                },
                OnLlmRequestJson: sessionRequestStore is not null && sessionId is not null
                    ? (idx, json) => sessionRequestStore.SaveLlmRequestAsync(sessionId, requestKey + $"_r{idx}", json)
                    : null,
                OnLlmResponseJson: sessionRequestStore is not null && sessionId is not null
                    ? (idx, json) => sessionRequestStore.SaveLlmResponseAsync(sessionId, requestKey + $"_r{idx}", json)
                    : null,
                Tools: resolvedTools.Count > 0 ? resolvedTools : null,
                SystemPrompt: agent is not null ? () => agent.SystemPrompt : null,
                LlmConfig: agent?.LlmConfig,
                SessionPath: sessionStore is not null && sessionId is not null
                    ? sessionStore.GetSessionDir(sessionId)
                    : null);

            var result = await handler.RunServerChatAsync(chatOpts, requestCts.Token);

            var doneJson = JsonSerializer.Serialize(new
            {
                choices = new[] { new { delta = new { content = "" } } },
                usage = result.Usage == null ? null : (object)new
                {
                    promptTokens = result.Usage.InputTokens,
                    completionTokens = result.Usage.OutputTokens,
                },
                bashgpt = new
                {
                    @event = "done",
                    response = result.Response,
                    usedToolCalls = result.UsedToolCalls,
                    finalStatus = result.FinalStatus,
                    requestId,
                    logs = result.Logs,
                    commands = result.Commands,
                },
            }, JsonDefaults.Options);
            ApiResponse.WriteSseEvent(stream, doneJson);
            ApiResponse.WriteSseEvent(stream, "[DONE]");

            if (sessionStore is not null && sessionId is not null)
            {
                var newMessages = BuildSessionMessages(body.Prompt.Trim(), result);

                var existingMessages = session?.Messages ?? [];
                var allMessages = existingMessages.Concat(newMessages).ToList();
                var title = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
                if (title.Length > 40) title = title[..40] + "...";

                await sessionStore.UpsertAsync(new SessionRecord
                {
                    Id = sessionId,
                    Title = title,
                    CreatedAt = session?.CreatedAt ?? now,
                    UpdatedAt = now,
                    Messages = allMessages,
                    EnabledTools = selectableToolNames?.ToList(),
                    AgentId = agent?.Id ?? session?.AgentId,
                });

                var reqRecord = new SessionRequestRecord
                {
                    Timestamp = requestKey,
                    Request = new SessionRequestData { Prompt = body.Prompt.Trim() },
                    Response = new SessionResponseData
                    {
                        Content = result.Response,
                        Commands = ToSessionCommands(result.Commands),
                        Usage = result.Usage is null ? null : new SessionTokenUsage
                        {
                            InputTokens = result.Usage.InputTokens,
                            OutputTokens = result.Usage.OutputTokens,
                            TotalTokens = result.Usage.TotalTokens,
                            CachedInputTokens = result.Usage.CachedInputTokens,
                        },
                    },
                };
                if (sessionRequestStore is not null)
                    await sessionRequestStore.SaveRequestAsync(sessionId, reqRecord);
            }
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try
            {
                var cancelledJson = JsonSerializer.Serialize(new
                {
                    choices = new[] { new { delta = new { content = "" } } },
                    bashgpt = new
                    {
                        @event = "done",
                        response = "Cancelled by user.",
                        usedToolCalls = false,
                        finalStatus = "user_cancelled",
                        requestId,
                        logs = Array.Empty<string>(),
                        commands = Array.Empty<object>(),
                    },
                }, JsonDefaults.Options);
                ApiResponse.WriteSseEvent(stream, cancelledJson);
                ApiResponse.WriteSseEvent(stream, "[DONE]");
            }
            catch { }
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"[server] Streaming request failed: {ex}");
                var errJson = JsonSerializer.Serialize(
                    new { choices = new[] { new { delta = new { content = "", bashgpt = new { @event = "error", data = new { message = ApiErrors.GenericServerError } } } } } },
                    JsonDefaults.Options);
                ApiResponse.WriteSseEvent(stream, errJson);
                ApiResponse.WriteSseEvent(stream, "[DONE]");
            }
            catch { }
        }
        finally
        {
            runningChats.Unregister(requestId);
            ctx.Response.Close();
        }
    }

    private string? ResolveSessionId(string? requestedSessionId) =>
        sessionStore is null
            ? null
            : string.IsNullOrWhiteSpace(requestedSessionId)
                ? SessionStore.LiveSessionId
                : requestedSessionId;

    private static List<SessionCommand>? ToSessionCommands(IReadOnlyList<CommandResult>? commands)
        => commands is not { Count: > 0 }
            ? null
            : commands.Select(c => new SessionCommand
            {
                Command = c.Command,
                ExitCode = c.ExitCode,
                Output = c.Output,
                WasExecuted = c.WasExecuted,
            }).ToList();

    private static List<ChatMessage> BuildConversationDelta(ServerChatResult result)
    {
        if (result.ConversationDelta is { Count: > 0 })
            return result.ConversationDelta.ToList();

        return [new ChatMessage(ChatRole.Assistant, result.Response)];
    }

    private static List<SessionMessage> BuildSessionMessages(string prompt, ServerChatResult result)
    {
        var messages = new List<SessionMessage>
        {
            new() { Role = "user", Content = prompt }
        };

        messages.AddRange(BuildConversationDelta(result).Select(SessionMessageMapper.FromChatMessage));

        var finalAssistant = messages.LastOrDefault(m => m.Role == "assistant" && (m.ToolCalls is null || m.ToolCalls.Count == 0));
        if (finalAssistant is null)
        {
            finalAssistant = new SessionMessage { Role = "assistant", Content = result.Response };
            messages.Add(finalAssistant);
        }

        finalAssistant.Commands = ToSessionCommands(result.Commands);
        finalAssistant.Usage = result.Usage is null ? null : new SessionTokenUsage
        {
            InputTokens = result.Usage.InputTokens,
            OutputTokens = result.Usage.OutputTokens,
            TotalTokens = result.Usage.TotalTokens,
            CachedInputTokens = result.Usage.CachedInputTokens,
        };

        return messages;
    }

    private sealed record ChatRequest(
        string Prompt,
        bool? Verbose,
        string? SessionId,
        string[]? EnabledTools,
        string? AgentId = null,
        string? RequestId = null);
}
