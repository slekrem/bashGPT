using System.Diagnostics;
using System.Text;
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
        Temperature:       0.1,    // deterministisch – Code ist kein kreativer Output
        TopP:              0.95,
        NumCtx:            65536,  // 64k Kontext für Dateien, Diffs und Logs
        MaxTokens:         8192,   // Output: genug für komplexe Code-Antworten
        ReasoningEffort:   "high", // komplexe Aufgaben brauchen gutes Reasoning
        FrequencyPenalty:  0.1,    // repetitive Tool-Call-Schleifen dämpfen
        ParallelToolCalls: false,  // sequenziell – sicherer bei Dateimutationen
        Stream:            true
    );

    public override IReadOnlyList<string> SystemPrompt =>
    [
        """
        Du bist ein erfahrener Software-Entwickler. Loese Aufgaben durch gezielten Tool-Einsatz.
        """,
        """
        Tool-Call-Regeln:
        - Halte dich strikt an das Schema: richtige Typen, alle Pflichtfelder, gueltige Werte.
        - Schlaegt ein Tool mit "missing_required_field" fehl: fuege genau dieses Feld hinzu und wiederhole.
        - Schlaegt ein Tool mit "invalid_type" oder "invalid_value" fehl: korrigiere nur das benannte Feld.
        - Schlaegt ein Tool mit "invalid_json" fehl: sende gueltiges JSON und wiederhole.
        - Fehlende optionale Pfade: setze "path": "." als Default.
        """,
        BuildProjectContext(),
    ];

    /// <summary>
    /// Generiert zur Laufzeit einen Projektkontext: Git-Status, Verzeichnisstruktur und CLAUDE.md.
    /// Wird bei jedem Chat-Request frisch aufgebaut.
    /// </summary>
    private static string BuildProjectContext()
    {
        var cwd = Directory.GetCurrentDirectory();
        var sb  = new StringBuilder("# Projektkontext\n\n");

        // Arbeitsverzeichnis + Git
        sb.AppendLine($"**Verzeichnis:** `{cwd}`\n");
        var branch     = Git("rev-parse --abbrev-ref HEAD");
        var lastCommit = Git("log -1 --oneline");
        if (branch is not null)
        {
            sb.AppendLine("**Git:**");
            sb.AppendLine($"- Branch: `{branch}`");
            if (lastCommit is not null)
                sb.AppendLine($"- Letzter Commit: `{lastCommit}`");
            sb.AppendLine();
        }

        // src/-Struktur
        var srcDir = Path.Combine(cwd, "src");
        if (Directory.Exists(srcDir))
        {
            sb.AppendLine("**Projekte (src/):**");
            foreach (var dir in Directory.GetDirectories(srcDir).Order())
                sb.AppendLine($"- `{Path.GetFileName(dir)}/`");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string? Git(string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            return proc?.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch { return null; }
    }

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
