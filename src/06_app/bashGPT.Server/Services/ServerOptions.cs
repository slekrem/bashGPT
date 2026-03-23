namespace bashGPT.Server.Services;

public record ServerOptions(
    int Port,
    bool NoBrowser,
    string? Model,
    bool Verbose
);
