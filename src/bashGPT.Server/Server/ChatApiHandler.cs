using System.Net;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Providers;
using BashGPT.Shell;
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
                .Where(m => m.Role is "user" or "assistant")
                .Select(m => new ChatMessage(
                    m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                    m.Content))
                .ToList() ?? [];
        }
        else
        {
            historySnapshot = legacyHistory.GetSnapshot();
        }

        var requestedMode = ExecModeConverter.Parse(body.ExecMode) ?? state.ExecMode;
        var chatOpts = new ServerChatOptions(
            Prompt:     body.Prompt.Trim(),
            History:    historySnapshot,
            Provider:   options.Provider,
            Model:      options.Model,
            NoContext:  options.NoContext,
            IncludeDir: options.IncludeDir,
            ExecMode:   requestedMode,
            Verbose:    options.Verbose || body.Verbose == true,
            ForceTools: state.ForceTools);

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
            var newMessages = new List<SessionMessage>
            {
                new() { Role = "user", Content = body.Prompt.Trim(), ExecMode = body.ExecMode },
                new()
                {
                    Role     = "assistant",
                    Content  = result.Response,
                    Usage    = result.Usage is null ? null : new SessionTokenUsage
                    {
                        InputTokens       = result.Usage.InputTokens,
                        OutputTokens      = result.Usage.OutputTokens,
                        TotalTokens       = result.Usage.TotalTokens,
                        CachedInputTokens = result.Usage.CachedInputTokens,
                    },
                    Commands = result.Commands.Count > 0
                        ? result.Commands.Select(c => new SessionCommand
                          {
                              Command     = c.Command,
                              ExitCode    = c.ExitCode,
                              Output      = c.Output,
                              WasExecuted = c.WasExecuted,
                          }).ToList()
                        : null,
                },
            };

            var existingMessages = session?.Messages ?? [];
            var allMessages      = existingMessages.Concat(newMessages).ToList();
            var title            = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
            if (title.Length > 40) title = title[..40] + "…";

            var now = DateTime.UtcNow.ToString("o");
            await sessionStore.UpsertAsync(new SessionRecord
            {
                Id           = body.SessionId,
                Title        = title,
                CreatedAt    = session?.CreatedAt ?? now,
                UpdatedAt    = now,
                Messages     = allMessages,
                ShellContext = shellCtx,
            });

            await sessionStore.SaveRequestAsync(body.SessionId, new SessionRequestRecord
            {
                Timestamp = now,
                Prompt    = body.Prompt.Trim(),
                ExecMode  = body.ExecMode,
                Response  = result.Response,
                Commands  = result.Commands.Count > 0
                    ? result.Commands.Select(c => new SessionCommand
                      {
                          Command     = c.Command,
                          ExitCode    = c.ExitCode,
                          Output      = c.Output,
                          WasExecuted = c.WasExecuted,
                      }).ToList()
                    : null,
                Usage = result.Usage is null ? null : new SessionTokenUsage
                {
                    InputTokens       = result.Usage.InputTokens,
                    OutputTokens      = result.Usage.OutputTokens,
                    TotalTokens       = result.Usage.TotalTokens,
                    CachedInputTokens = result.Usage.CachedInputTokens,
                },
            });
        }
        else
        {
            // Fallback: globale In-Memory-History (legacy, kein SessionStore)
            legacyHistory.Append(new ChatMessage(ChatRole.User,      body.Prompt.Trim()));
            legacyHistory.Append(new ChatMessage(ChatRole.Assistant, result.Response));
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new
        {
            response      = result.Response,
            usedToolCalls = result.UsedToolCalls,
            logs          = result.Logs,
            shellContext  = new { user = shellCtx.User, host = shellCtx.Host, cwd = shellCtx.Cwd },
            usage         = result.Usage == null ? null : (object)new
            {
                inputTokens       = result.Usage.InputTokens,
                outputTokens      = result.Usage.OutputTokens,
                totalTokens       = result.Usage.TotalTokens,
                cachedInputTokens = result.Usage.CachedInputTokens,
            },
            commands = result.Commands.Select(c => new
            {
                command     = c.Command,
                exitCode    = c.ExitCode,
                output      = c.Output,
                wasExecuted = c.WasExecuted,
            }),
        });
    }

    private sealed record ChatRequest(string Prompt, string? ExecMode, bool? Verbose, string? SessionId);
}
