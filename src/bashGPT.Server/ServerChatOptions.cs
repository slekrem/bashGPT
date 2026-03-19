using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Server;

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
    IReadOnlyList<ToolDefinition>? Tools = null,
    Func<IReadOnlyList<string>>? SystemPrompt = null,
    AgentLlmConfig? LlmConfig = null,
    string? SessionPath = null
);

public record LlmExchangeRecord(string? RequestJson, string? ResponseJson);

public record ServerChatResult(
    string Response,
    IReadOnlyList<string> Logs,
    TokenUsage? Usage = null,
    IReadOnlyList<LlmExchangeRecord>? LlmExchanges = null,
    IReadOnlyList<CommandResult>? Commands = null,
    bool UsedToolCalls = false,
    IReadOnlyList<ChatMessage>? ConversationDelta = null,
    string FinalStatus = "completed"
);

public record SseEvent(string Event, object? Data = null);
