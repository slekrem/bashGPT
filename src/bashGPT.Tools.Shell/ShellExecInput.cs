namespace BashGPT.Tools.Shell;

public sealed record ShellExecInput(
    string Command,
    string? Cwd = null,
    int TimeoutMs = 5000,
    IReadOnlyDictionary<string, string>? Env = null);
