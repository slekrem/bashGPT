using BashGPT.Configuration;
using BashGPT.Shell;

namespace BashGPT.Server;

public record ServerOptions(
    int Port,
    bool NoBrowser,
    ProviderType? Provider,
    string? Model,
    bool NoContext,
    bool IncludeDir,
    ExecutionMode ExecMode,
    bool Verbose,
    bool ForceTools
);
