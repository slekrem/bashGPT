using bashGPT.Agents;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Configuration;

namespace bashGPT.Server.Models;

public record ServerChatOptions(
    string Prompt,
    IReadOnlyList<ChatMessage> History,
    string? Model,
    bool Verbose,
    Action<string>? OnToken = null,
    Action<string>? OnReasoningToken = null,
    Action<SseEvent>? OnEvent = null,
    Func<int, string, Task>? OnLlmRequestJson = null,
    Func<int, string, Task>? OnLlmResponseJson = null,
    IReadOnlyList<ProviderToolDefinition>? Tools = null,
    Func<string?, IReadOnlyList<string>>? SystemPrompt = null,
    AgentLlmConfig? LlmConfig = null,
    string? SessionPath = null,
    AgentBase? Agent = null
);

public record ServerChatResult(
    string Response,
    IReadOnlyList<string> Logs,
    TokenUsage? Usage = null,
    IReadOnlyList<LlmExchangeRecord>? LlmExchanges = null,
    IReadOnlyList<SessionCommand>? Commands = null,
    bool UsedToolCalls = false,
    IReadOnlyList<ChatMessage>? ConversationDelta = null,
    string FinalStatus = "completed"
);

public record SseEvent(string Event, object? Data = null);
