using System.Net;
using System.Text.Json;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Cli;
using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

internal sealed class ChatApiHandler(
    IPromptHandler handler,
    LegacyHistory legacyHistory,
    SessionStore? sessionStore = null,
    ToolRegistry? toolRegistry = null,
    AgentRegistry? agentRegistry = null)
{
    public async Task HandleAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<ChatRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Prompt fehlt." }, statusCode: 400);
            return;
        }

        // Agent laden (optional)
        AgentBase? agent = null;
        if (agentRegistry is not null && !string.IsNullOrWhiteSpace(body.AgentId))
            agent = agentRegistry.Find(body.AgentId);

        // Session laden (einmalig – wird für History, EnabledTools und Persistenz genutzt)
        SessionRecord? session = null;
        if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
            session = await sessionStore.LoadAsync(body.SessionId);

        // History laden: session-basiert oder globaler Fallback
        IReadOnlyList<ChatMessage> historySnapshot;
        if (session is not null)
        {
            historySnapshot = session.Messages
                .Select(SessionMessageMapper.ToChatMessage)
                .OfType<ChatMessage>()
                .ToList();
        }
        else if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
        {
            historySnapshot = [];
        }
        else
        {
            historySnapshot = legacyHistory.GetSnapshot();
        }

        // EnabledTools: Agent-Wert hat Vorrang, danach Session-Wert, danach Request-Wert
        var effectiveToolNames = agent?.EnabledTools.Count > 0
            ? agent.EnabledTools.ToList()
            : session?.EnabledTools?.Count > 0
                ? session.EnabledTools
                : body.EnabledTools?.ToList();

        var resolvedTools = ToolHelper.Resolve(effectiveToolNames, toolRegistry);

        var now        = DateTime.UtcNow.ToString("o");
        var requestKey = now + "_" + Guid.NewGuid().ToString("N")[..8];
        var sessionId  = body.SessionId;

        // Session-Pfad setzen, bevor agent.SystemPrompt ausgewertet wird.
        ContextFileCache.CurrentSessionPath = sessionStore is not null && !string.IsNullOrWhiteSpace(sessionId)
            ? sessionStore.GetSessionDir(sessionId)
            : null;

        var chatOpts = new ServerChatOptions(
            Prompt:   body.Prompt.Trim(),
            History:  historySnapshot,
            Provider: options.Provider,
            Model:    options.Model,
            Verbose:  options.Verbose || body.Verbose == true,
            OnLlmRequestJson: sessionStore is not null && !string.IsNullOrWhiteSpace(sessionId)
                ? (idx, json) => sessionStore.SaveLlmRequestAsync(sessionId, requestKey + $"_r{idx}", json)
                : null,
            OnLlmResponseJson: sessionStore is not null && !string.IsNullOrWhiteSpace(sessionId)
                ? (idx, json) => sessionStore.SaveLlmResponseAsync(sessionId, requestKey + $"_r{idx}", json)
                : null,
            Tools:        resolvedTools.Count > 0 ? resolvedTools : null,
            SystemPrompt: agent is not null ? () => agent.SystemPrompt : null,
            SessionPath:  sessionStore is not null && !string.IsNullOrWhiteSpace(sessionId)
                ? sessionStore.GetSessionDir(sessionId)
                : null);

        var result = await handler.RunServerChatAsync(chatOpts, ct);

        var shellCtx = new SessionShellContext
        {
            User = Environment.UserName,
            Host = Environment.MachineName,
            Cwd  = Environment.CurrentDirectory,
        };

        // Persistieren: session-basiert oder globaler Fallback
        if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
        {
            var newMessages = BuildSessionMessages(body.Prompt.Trim(), result);

            var existingMessages = session?.Messages ?? [];
            var allMessages      = existingMessages.Concat(newMessages).ToList();
            var title            = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
            if (title.Length > 40) title = title[..40] + "...";

            await sessionStore.UpsertAsync(new SessionRecord
            {
                Id           = body.SessionId,
                Title        = title,
                CreatedAt    = session?.CreatedAt ?? now,
                UpdatedAt    = now,
                Messages     = allMessages,
                ShellContext = shellCtx,
                EnabledTools = effectiveToolNames,
                AgentId      = agent?.Id ?? session?.AgentId,
            });

            var reqRecord = new SessionRequestRecord
            {
                Timestamp = requestKey,
                Request   = new SessionRequestData { Prompt = body.Prompt.Trim() },
                Response  = new SessionResponseData
                {
                    Content  = result.Response,
                    Commands = ToSessionCommands(result.Commands),
                    Usage    = result.Usage is null ? null : new SessionTokenUsage
                    {
                        InputTokens       = result.Usage.InputTokens,
                        OutputTokens      = result.Usage.OutputTokens,
                        TotalTokens       = result.Usage.TotalTokens,
                        CachedInputTokens = result.Usage.CachedInputTokens,
                    },
                },
            };
            await sessionStore.SaveRequestAsync(body.SessionId, reqRecord);
        }
        else
        {
            // Fallback: globale In-Memory-History (legacy, kein SessionStore)
            legacyHistory.Append(new ChatMessage(ChatRole.User, body.Prompt.Trim()));
            foreach (var msg in BuildConversationDelta(result))
                legacyHistory.Append(msg);
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new
        {
            response     = result.Response,
            usedToolCalls = result.UsedToolCalls,
            finalStatus  = result.FinalStatus,
            logs         = result.Logs,
            commands     = result.Commands,
            shellContext = new { user = shellCtx.User, host = shellCtx.Host, cwd = shellCtx.Cwd },
            usage        = result.Usage == null ? null : (object)new
            {
                inputTokens       = result.Usage.InputTokens,
                outputTokens      = result.Usage.OutputTokens,
                totalTokens       = result.Usage.TotalTokens,
                cachedInputTokens = result.Usage.CachedInputTokens,
            },
        });
    }

    private static List<SessionCommand>? ToSessionCommands(IReadOnlyList<CommandResult>? commands)
        => commands is not { Count: > 0 }
            ? null
            : commands.Select(c => new SessionCommand
            {
                Command     = c.Command,
                ExitCode    = c.ExitCode,
                Output      = c.Output,
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
        finalAssistant.Usage    = result.Usage is null ? null : new SessionTokenUsage
        {
            InputTokens       = result.Usage.InputTokens,
            OutputTokens      = result.Usage.OutputTokens,
            TotalTokens       = result.Usage.TotalTokens,
            CachedInputTokens = result.Usage.CachedInputTokens,
        };

        return messages;
    }

    private sealed record ChatRequest(string Prompt, bool? Verbose, string? SessionId, string[]? EnabledTools, string? AgentId = null);
}
