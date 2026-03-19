using BashGPT.Configuration;

namespace BashGPT.Cli;

/// <summary>Geparste CLI-Optionen für den Hauptbefehl.</summary>
public record CliOptions(
    string Prompt,
    ProviderType? Provider,
    string? Model,
    bool NoContext,
    bool IncludeDir,
    bool Verbose,
    bool? ForceTools
);
