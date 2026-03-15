using BashGPT.Agents;
using BashGPT.Providers;

namespace BashGPT.Agents.Dev;

/// <summary>
/// Spezialisierter Entwicklungsagent mit Zugriff auf Filesystem, Git, Build und Test-Tools.
/// </summary>
public sealed class DevAgent : AgentBase
{
    public override string Id => "dev";

    public override string Name => "Dev-Agent";

    public override IReadOnlyList<string> EnabledTools =>
    [
        "fetch",
        "filesystem_read",
        "filesystem_write",
        "filesystem_search",
        "git_status",
        "git_diff",
        "git_log",
        "git_branch",
        "git_add",
        "git_commit",
        "git_checkout",
        "test_run",
        "build_run",
        "shell_exec",
    ];

    public override AgentLlmConfig LlmConfig => new(
        Temperature:       0.2,
        TopP:              0.9,
        NumCtx:            8192,
        MaxTokens:         4096,
        ReasoningEffort:   "medium",
        ParallelToolCalls: false,
        Stream:            true
    );

    public override string SystemPrompt => """
        Du bist ein erfahrener Software-Entwickler. Loese Aufgaben durch gezielten Tool-Einsatz.

        Tool-Calls:
        - Halte dich strikt an das Schema: richtige Typen, alle Pflichtfelder, gueltige Werte.
        - Schlaegt ein Tool mit "missing_required_field" fehl: fuege genau dieses Feld hinzu und wiederhole.
        - Schlaegt ein Tool mit "invalid_type" oder "invalid_value" fehl: korrigiere nur das benannte Feld.
        - Schlaegt ein Tool mit "invalid_json" fehl: sende gueltiges JSON und wiederhole.
        - Fehlende optionale Pfade: setze "path": "." als Default.
        """;

    protected override string GetAgentMarkdown() => """
        # Dev-Agent

        Spezialisierter Software-Entwicklungsagent mit vollständigem Zugriff auf Dateisystem, Git, Build- und Test-Tools.

        ## Fähigkeiten

        - Dateien lesen, schreiben und durchsuchen
        - Git-Operationen (Status, Diff, Log, Branch, Commit, Checkout)
        - Builds ausführen und Testergebnisse auswerten
        - Shell-Befehle ausführen
        - Web-Inhalte abrufen (fetch)

        ## Aktive Tools

        | Tool | Beschreibung |
        |---|---|
        | `fetch` | Web-Inhalte abrufen |
        | `filesystem_read` | Dateien und Verzeichnisse lesen |
        | `filesystem_write` | Dateien erstellen und bearbeiten |
        | `filesystem_search` | Dateien nach Muster durchsuchen |
        | `git_status` | Git-Status anzeigen |
        | `git_diff` | Änderungen vergleichen |
        | `git_log` | Commit-Historie einsehen |
        | `git_branch` | Branches verwalten |
        | `git_add` | Änderungen stagen |
        | `git_commit` | Commits erstellen |
        | `git_checkout` | Branches wechseln |
        | `test_run` | Tests ausführen |
        | `build_run` | Build starten |
        | `shell_exec` | Shell-Befehle ausführen |

        ## Hinweise

        Dieser Agent folgt strengen Regeln für Tool-Calls und wiederholt fehlerhafte Aufrufe automatisch mit korrigierten Argumenten.
        """;
}
