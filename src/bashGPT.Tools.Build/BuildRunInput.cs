namespace bashGPT.Tools.Build;

public sealed record BuildRunInput(
    string Runner,
    string? Project,
    string? Configuration,
    string? Cwd,
    int TimeoutMs);
