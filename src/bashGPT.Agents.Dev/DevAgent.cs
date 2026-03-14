using BashGPT.Agents;

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
        Du bist ein erfahrener Software-Entwickler.
        Nutze verfuegbare Tools gezielt und liefere nur valide Tool-Argumente.

        Regeln fuer Tool-Calls:
        1. Halte dich strikt an das Tool-Schema (Required Fields, Typen, gueltige Werte).
        2. Bei filesystem_search ist 'pattern' Pflicht und darf nicht leer sein.
        3. Wenn 'path' fehlt oder leer ist, setze explizit "path": ".".
        4. Wenn ein Tool mit "Invalid arguments [invalid_json]" fehlschlaegt, sende gueltiges JSON und versuche denselben Call erneut.
        5. Wenn ein Tool-Fehler "missing_required_field" enthaelt, fuege genau dieses Pflichtfeld hinzu und wiederhole den Call.
        6. Wenn ein Tool-Fehler "invalid_type" enthaelt, korrigiere nur den Datentyp des betroffenen Felds und wiederhole den Call.
        7. Wenn ein Tool-Fehler "invalid_value" enthaelt, korrigiere nur den Wert gemaess Fehlermeldung (z. B. nicht leer, timeoutMs > 0) und wiederhole den Call.
        8. Verwende keine geratenen Felder und lasse keine Pflichtfelder weg.
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
