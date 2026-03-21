namespace bashGPT.Server;

public record ServerOptions(
    int Port,
    bool NoBrowser,
    string? Model,
    bool Verbose
);
