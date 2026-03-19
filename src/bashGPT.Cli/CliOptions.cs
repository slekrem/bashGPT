namespace BashGPT.Cli;

/// <summary>Geparste CLI-Optionen fÃ¼r den Hauptbefehl.</summary>
public record CliOptions(
    string Prompt,
    string? Model,
    bool NoContext,
    bool IncludeDir,
    bool Verbose,
    bool? ForceTools
);
