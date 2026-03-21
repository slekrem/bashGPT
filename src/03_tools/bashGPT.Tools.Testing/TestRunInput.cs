namespace bashGPT.Tools.Testing;

public sealed record TestRunInput(
    string Runner,
    string? Project,
    string? Filter,
    string? Cwd,
    int TimeoutMs);
