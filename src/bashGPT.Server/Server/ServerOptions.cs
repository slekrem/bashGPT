using BashGPT.Configuration;

namespace BashGPT.Server;

public record ServerOptions(
    int Port,
    bool NoBrowser,
    ProviderType? Provider,
    string? Model,
    bool Verbose
);
