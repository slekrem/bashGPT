namespace bashGPT.Tools.Shell;

public sealed record ShellExecOutput(
    string Stdout,
    string Stderr,
    int ExitCode,
    long DurationMs,
    bool TimedOut);
