namespace BashGPT.Agents;

/// <summary>
/// Abstrakte Basisklasse für alle Chat-Agenten.
/// Jeder Agent definiert sich vollständig durch Code – keine JSON-Konfiguration.
/// Aus der Klassendefinition werden sowohl das LLM-System-Prompt als auch
/// das Info-Panel der Web-UI abgeleitet.
/// </summary>
public abstract class AgentBase
{
    /// <summary>Eindeutige, stabile Kennung des Agenten (z. B. "dev", "shell").</summary>
    public abstract string Id { get; }

    /// <summary>Anzeigename des Agenten in der UI.</summary>
    public abstract string Name { get; }

    /// <summary>Liste der Tool-Namen, die für diesen Agenten aktiv sind.</summary>
    public abstract IReadOnlyList<string> EnabledTools { get; }

    /// <summary>System-Prompt, der dem LLM bei jedem Chat übergeben wird.</summary>
    public abstract string SystemPrompt { get; }

    /// <summary>
    /// Markdown-Dokument, das im Info-Panel der Web-UI angezeigt wird.
    /// Beschreibt den Agenten, seine Fähigkeiten und aktiven Tools.
    /// </summary>
    public abstract string GetInfoPanelMarkdown();
}
