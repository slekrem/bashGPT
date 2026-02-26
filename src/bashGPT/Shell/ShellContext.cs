namespace BashGPT.Shell;

public record ShellContext(
    string WorkingDirectory,
    string OperatingSystem,
    string Shell,
    GitContext? Git,
    IReadOnlyList<string> DirectoryEntries,
    IReadOnlyDictionary<string, string> Environment
);

public record GitContext(
    string Branch,
    string? LastCommit,
    IReadOnlyList<string> ChangedFiles
);
