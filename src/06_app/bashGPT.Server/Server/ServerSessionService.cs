using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class ServerSessionService(
    SessionStore? sessionStore = null,
    SessionRequestStore? sessionRequestStore = null)
{
    public string? ResolveSessionId(string? requestedSessionId) =>
        sessionStore is null
            ? null
            : string.IsNullOrWhiteSpace(requestedSessionId)
                ? SessionStore.LiveSessionId
                : requestedSessionId;

    public Task<SessionRecord?> LoadAsync(string? sessionId) =>
        sessionStore is not null && sessionId is not null
            ? sessionStore.LoadAsync(sessionId)
            : Task.FromResult<SessionRecord?>(null);

    public IReadOnlyList<ChatMessage> BuildHistorySnapshot(SessionRecord? session) =>
        session is null
            ? []
            : session.Messages
                .Select(SessionMessageMapper.ToChatMessage)
                .OfType<ChatMessage>()
                .ToList();

    public string? GetSessionPath(string? sessionId) =>
        sessionStore is not null && sessionId is not null
            ? sessionStore.GetSessionDir(sessionId)
            : null;

    public Task SaveLlmRequestAsync(string sessionId, string key, int exchangeIndex, string json) =>
        sessionRequestStore?.SaveLlmRequestAsync(sessionId, $"{key}_r{exchangeIndex}", json)
        ?? Task.CompletedTask;

    public Task SaveLlmResponseAsync(string sessionId, string key, int exchangeIndex, string json) =>
        sessionRequestStore?.SaveLlmResponseAsync(sessionId, $"{key}_r{exchangeIndex}", json)
        ?? Task.CompletedTask;

    public async Task PersistChatAsync(
        string sessionId,
        string prompt,
        string requestKey,
        string now,
        IReadOnlyList<string>? enabledTools,
        string? agentId,
        SessionRecord? existingSession,
        ServerChatResult result)
    {
        if (sessionStore is null)
            return;

        var newMessages = BuildSessionMessages(prompt, result);
        var existingMessages = existingSession?.Messages ?? [];
        var allMessages = existingMessages.Concat(newMessages).ToList();
        var title = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
        if (title.Length > 40) title = title[..40] + "...";

        await sessionStore.UpsertAsync(new SessionRecord
        {
            Id = sessionId,
            Title = title,
            CreatedAt = existingSession?.CreatedAt ?? now,
            UpdatedAt = now,
            Messages = allMessages,
            EnabledTools = enabledTools?.ToList(),
            AgentId = agentId ?? existingSession?.AgentId,
        });

        if (sessionRequestStore is null)
            return;

        var requestRecord = new SessionRequestRecord
        {
            Timestamp = requestKey,
            Request = new SessionRequestData { Prompt = prompt },
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

        await sessionRequestStore.SaveRequestAsync(sessionId, requestRecord);
    }

    private static List<SessionCommand>? ToSessionCommands(IReadOnlyList<SessionCommand>? commands)
        => commands is not { Count: > 0 } ? null : commands.ToList();

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
}
