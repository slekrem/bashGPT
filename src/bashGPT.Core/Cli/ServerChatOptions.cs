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
    bool ForceTools
);

public record ServerChatResult(
    string Response,
    IReadOnlyList<CommandResult> Commands,
    IReadOnlyList<string> Logs,
    bool UsedToolCalls,
    TokenUsage? Usage = null
);
