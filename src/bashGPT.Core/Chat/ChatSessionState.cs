using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace bashGPT.Core.Chat;

public sealed class ChatSessionState(
    ILlmProvider provider,
    IReadOnlyList<ToolDefinition> tools,
    string? toolChoiceName = null,
    Func<IReadOnlyList<string>>? systemPrompt = null,
    AgentLlmConfig? llmConfig = null,
    Action<string>? onReasoningToken = null,
    Func<int, string, Task>? onLlmRequestJson = null,
    Func<int, string, Task>? onLlmResponseJson = null)
{
    public ILlmProvider Provider { get; } = provider;
    public List<ChatMessage> Messages { get; } = [];
    public IReadOnlyList<ToolDefinition> Tools { get; } = tools;
    public string? ToolChoiceName { get; } = toolChoiceName;
    public List<LlmExchangeRecord> LlmExchanges { get; } = [];
    public LlmChatResponse? LastResponse { get; private set; }
    public int TotalInputTokens { get; private set; }
    public int TotalOutputTokens { get; private set; }

    public void InitializeMessages(IEnumerable<ChatMessage> history, string prompt)
    {
        Messages.Clear();
        RefreshSystemMessages();
        Messages.AddRange(history);
        Messages.Add(new ChatMessage(ChatRole.User, prompt));
    }

    public void RefreshSystemMessages()
    {
        if (systemPrompt is null)
            return;

        var firstNonSystem = Messages.FindIndex(m => m.Role != ChatRole.System);
        var removeCount = firstNonSystem >= 0 ? firstNonSystem : Messages.Count;
        if (removeCount > 0)
            Messages.RemoveRange(0, removeCount);

        var freshPrompts = systemPrompt()
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        for (var i = freshPrompts.Count - 1; i >= 0; i--)
            Messages.Insert(0, new ChatMessage(ChatRole.System, freshPrompts[i]));
    }

    public async Task<(LlmChatResponse Response, string? Error)> CallOnceAsync(
        Action<string>? onToken,
        CancellationToken ct)
    {
        var exchangeIndex = LlmExchanges.Count;
        string? requestJson = null;
        string? responseJson = null;

        var result = await ChatOrchestrator.ChatOnceAsync(
            Provider,
            Messages,
            Tools,
            ToolChoiceName,
            ct,
            onToken,
            onReasoningToken,
            onRequestJson: async json =>
            {
                requestJson = json;
                if (onLlmRequestJson is not null)
                    await onLlmRequestJson(exchangeIndex, json);
            },
            onResponseJson: async json =>
            {
                responseJson = json;
                if (onLlmResponseJson is not null)
                    await onLlmResponseJson(exchangeIndex, json);
            },
            llmConfig);

        LlmExchanges.Add(new LlmExchangeRecord(requestJson, responseJson));

        if (result.Error is null)
        {
            LastResponse = result.Response;
            TotalInputTokens += result.Response.Usage?.InputTokens ?? 0;
            TotalOutputTokens += result.Response.Usage?.OutputTokens ?? 0;
        }

        return result;
    }

    public TokenUsage? BuildUsage() =>
        TotalInputTokens > 0 || TotalOutputTokens > 0
            ? new TokenUsage(TotalInputTokens, TotalOutputTokens)
            : null;

    public ChatSessionOutcome CreateCompletedOutcome(string finalStatus = "completed") =>
        new(
            Response: LastResponse?.Content ?? string.Empty,
            Usage: BuildUsage(),
            LlmExchanges: LlmExchanges.Count > 0 ? LlmExchanges : null,
            FinalStatus: finalStatus);

    public ChatSessionOutcome CreateCancelledOutcome(
        string response = "Cancelled by user.",
        string finalStatus = "user_cancelled") =>
        new(
            Response: response,
            Usage: BuildUsage(),
            LlmExchanges: LlmExchanges.Count > 0 ? LlmExchanges : null,
            FinalStatus: finalStatus);
}
