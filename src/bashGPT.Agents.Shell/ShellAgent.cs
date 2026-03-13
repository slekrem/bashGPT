using BashGPT.Agents;

namespace BashGPT.Agents.Shell;

/// <summary>
/// Shell-Assistent mit Fokus auf Terminal-Aufgaben und Shell-Befehle.
/// </summary>
public sealed class ShellAgent : AgentBase
{
    public override string Id => "shell";

    public override string Name => "Shell-Agent";

    public override IReadOnlyList<string> EnabledTools => ["shell_exec"];

    public override string SystemPrompt =>
        "Du bist ein erfahrener Shell-Assistent. Nutze das shell_exec-Tool, um Befehle auszufuehren und dem Benutzer bei Terminal-Aufgaben zu helfen. Erklaere, was du tust, und zeige Ergebnisse uebersichtlich.";

    public override string GetInfoPanelMarkdown() => """
        # Shell-Agent

        Spezialisierter Shell-Assistent für Terminal-Aufgaben.

        ## Fähigkeiten

        - Shell-Befehle ausführen und Ergebnisse erklären
        - Terminal-Workflows automatisieren
        - Systeminformationen abrufen

        ## Aktive Tools

        | Tool | Beschreibung |
        |---|---|
        | `shell_exec` | Shell-Befehle im aktuellen Arbeitsverzeichnis ausführen |

        ## Hinweise

        Dieser Agent erklärt jeden ausgeführten Befehl und zeigt Ergebnisse übersichtlich an.
        """;
}
