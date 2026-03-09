using System.Net;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Providers;
using BashGPT.Storage;

namespace BashGPT.Server;

internal sealed class ChatApiHandler(
    IPromptHandler handler,
    ServerState state,
    LegacyHistory legacyHistory,
    SessionStore? sessionStore = null)
{
    public async Task HandleAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<ChatRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Prompt fehlt." }, statusCode: 400);
            return;
        }

        // History laden: session-basiert oder globaler Fallback
        IReadOnlyList<ChatMessage> historySnapshot;
        if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
        {
            var session = await sessionStore.LoadAsync(body.SessionId);
            historySnapshot = session?.Messages
                .Select(SessionMessageMapper.ToChatMessage)
                .OfType<ChatMessage>()
                .ToList() ?? [];
        }
        else
        {
            historySnapshot = legacyHistory.GetSnapshot();
        }

        var now        = DateTime.UtcNow.ToString("o");
        var requestKey = now + "_" + Guid.NewGuid().ToString("N")[..8];
        var sessionId  = body.SessionId;

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
            var session     = await sessionStore.LoadAsync(body.SessionId);
            var newMessages = new List<SessionMessage>();

            newMessages.Add(new() { Role = "user", Content = body.Prompt.Trim() });
            newMessages.Add(new()
            {
                Role    = "assistant",
                Content = result.Response,
                Usage   = result.Usage is null ? null : new SessionTokenUsage
                {
                    InputTokens       = result.Usage.InputTokens,
                    OutputTokens      = result.Usage.OutputTokens,
                    TotalTokens       = result.Usage.TotalTokens,
                    CachedInputTokens = result.Usage.CachedInputTokens,
                },
            });

            var existingMessages = session?.Messages ?? [];
            var allMessages      = existingMessages.Concat(newMessages).ToList();
            var title            = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
            if (title.Length > 40) title = title[..40] + "…";

            await sessionStore.UpsertAsync(new SessionRecord
            {
                Id           = body.SessionId,
                Title        = title,
                CreatedAt    = session?.CreatedAt ?? now,
                UpdatedAt    = now,
                Messages     = allMessages,
                ShellContext = shellCtx,
            });

            var reqRecord = new SessionRequestRecord
            {
                Timestamp = requestKey,
                Request   = new SessionRequestData { Prompt = body.Prompt.Trim() },
                Response  = new SessionResponseData
                {
                    Content = result.Response,
                    Usage   = result.Usage is null ? null : new SessionTokenUsage
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
            legacyHistory.Append(new ChatMessage(ChatRole.User,      body.Prompt.Trim()));
            legacyHistory.Append(new ChatMessage(ChatRole.Assistant, result.Response));
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new
        {
            response     = result.Response,
            logs         = result.Logs,
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

    private sealed record ChatRequest(string Prompt, bool? Verbose, string? SessionId);
}
