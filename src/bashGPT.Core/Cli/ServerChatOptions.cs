using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

public record ServerChatOptions(
    string Prompt,
    IReadOnlyList<ChatMessage> History,
    ProviderType? Provider,
    string? Model,
    bool NoContext,
    bool IncludeDir,
    ExecutionMode ExecMode,
    bool Verbose,
    bool ForceTools,
    Action<string>? OnToken = null,
    Action<SseEvent>? OnEvent = null
);

/// <summary>
/// Rohes Request/Response-Paar eines einzelnen LLM-Aufrufs.
/// </summary>
public record LlmExchangeRecord(string? RequestJson, string? ResponseJson);

public record ServerChatResult(
    string Response,
    IReadOnlyList<CommandResult> Commands,
    IReadOnlyList<string> Logs,
    bool UsedToolCalls,
    TokenUsage? Usage = null,
    IReadOnlyList<LlmExchangeRecord>? LlmExchanges = null,
    IReadOnlyList<ChatMessage>? IntermediateMessages = null
);

public record SseEvent(string Event, object? Data = null);
