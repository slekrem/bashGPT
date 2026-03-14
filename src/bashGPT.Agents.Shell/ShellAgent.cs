using System.Runtime.InteropServices;
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

    public override AgentLlmConfig LlmConfig => new(
        Temperature: 0.1,
        TopP:        0.9,
        Stream:      true
    );

    public override string SystemPrompt =>
        $"""
        Du bist ein Shell-Executor. Fuehre Befehle aus – nichts weiter.

        Regeln:
        - Fuehre die Anweisung sofort aus, ohne sie zu erklaeren oder zu kommentieren.
        - Antworte ausschliesslich mit dem Ergebnis. Keine Vorschlaege, keine Erklaerungen, kein Smalltalk.
        - Mehrere Schritte: fuehre sie nacheinander aus, ohne Zwischenkommentare.
        - Nur nicht-interaktive Befehle (kein vim, top, less, tail -f).
        - Halte Ausgaben auf das Wesentliche beschraenkt (head, grep, etc.).
        - Destruktive Aktionen (rm -rf, Formatierung) nur bei expliziter Bestaetigung.

        Systemkontext:
        - Benutzer:    {Environment.UserName}
        - Maschine:    {Environment.MachineName}
        - OS:          {GetOsDescription()}
        - Shell:       {GetShell()}
        - Verzeichnis: {Directory.GetCurrentDirectory()}
        - Datum/Zeit:  {DateTime.Now:dd.MM.yyyy HH:mm:ss zzz}
        """;

    protected override string GetAgentMarkdown() =>
        $"""
        # Shell-Agent

        Spezialisierter Shell-Assistent für Terminal-Aufgaben.

        ## Systemkontext

        | Eigenschaft | Wert |
        |---|---|
        | `user` | `{Environment.UserName}` |
        | `host` | `{Environment.MachineName}` |
        | `os` | `{GetOsDescription()}` |
        | `shell` | `{GetShell()}` |
        | `cwd` | `{Directory.GetCurrentDirectory()}` |
        | `date` | `{DateTime.Now:dd.MM.yyyy}` |
        | `time` | `{DateTime.Now:HH:mm:ss zzz}` |

        ## Aktive Tools

        | Tool | Beschreibung |
        |---|---|
        | `shell_exec` | Shell-Befehle im aktuellen Arbeitsverzeichnis ausführen |

        ## Regeln

        - Nur nicht-interaktive Befehle (kein `vim`, `top`, `less`, `tail -f`)
        - Ausgaben auf das Wesentliche kürzen (`head`, `grep`, etc.)
        - Keine destruktiven Aktionen ohne explizite Bestätigung
        """;

    private static string GetOsDescription()
    {
        var desc = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))    return $"macOS {arch} – {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))  return $"Linux {arch} – {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"Windows {arch} – {desc}";
        return $"{desc} ({arch})";
    }

    private static string GetShell() =>
        Environment.GetEnvironmentVariable("SHELL")
        ?? Environment.GetEnvironmentVariable("ComSpec")
        ?? "unbekannt";
}
