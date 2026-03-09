using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Cli;

public record ServerChatOptions(
    string Prompt,
    IReadOnlyList<ChatMessage> History,
    ProviderType? Provider,
    string? Model,
    bool Verbose,
    Action<string>? OnToken = null,
    Action<string>? OnReasoningToken = null,
    Action<SseEvent>? OnEvent = null,
    Func<int, string, Task>? OnLlmRequestJson = null,
    Func<int, string, Task>? OnLlmResponseJson = null,
    IReadOnlyList<ToolDefinition>? Tools = null
);

/// <summary>
/// Rohes Request/Response-Paar eines einzelnen LLM-Aufrufs.
/// </summary>
public record LlmExchangeRecord(string? RequestJson, string? ResponseJson);

public record ServerChatResult(
    string Response,
    IReadOnlyList<string> Logs,
    TokenUsage? Usage = null,
    IReadOnlyList<LlmExchangeRecord>? LlmExchanges = null
);

public record SseEvent(string Event, object? Data = null);
