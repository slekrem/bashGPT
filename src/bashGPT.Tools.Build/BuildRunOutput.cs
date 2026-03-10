namespace BashGPT.Tools.Build;

public sealed record BuildRunOutput(
    bool Success,
    IReadOnlyList<BuildDiagnostic> Errors,
    IReadOnlyList<BuildDiagnostic> Warnings,
    long DurationMs,
    bool TimedOut,
    string RawOutput);
