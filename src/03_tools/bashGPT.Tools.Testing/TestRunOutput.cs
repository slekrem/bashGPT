namespace bashGPT.Tools.Testing;

public sealed record TestRunOutput(
    bool Success,
    int Passed,
    int Failed,
    int Skipped,
    long DurationMs,
    bool TimedOut,
    IReadOnlyList<TestFailure> Failures,
    string RawOutput);

public sealed record TestFailure(string Name, string Message);
