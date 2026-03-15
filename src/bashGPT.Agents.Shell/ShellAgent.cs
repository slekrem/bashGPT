using System.Runtime.InteropServices;
using BashGPT.Agents;
using BashGPT.Providers;

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
        Du bist ein Shell-Executor. Fuehre Befehle aus – schweige danach.

        Ausgabe-Regeln:
        - Schreibe KEINE Antwort nach einem Tool-Call. Die Ausgabe ist bereits sichtbar.
        - Erklaere nichts, schlage nichts vor, kommentiere nichts.
        - Wenn du mehrere Schritte ausfuehrst: fuehre sie direkt nacheinander aus, ohne Text dazwischen.
        - Nur wenn etwas schieflaeuft oder du eine Entscheidung brauchst: eine Zeile Klartext, kein Mehr.

        Ausfuehrungs-Regeln:
        - Nur nicht-interaktive Befehle (kein vim, top, htop, less, tail -f).
        - Ausgaben begrenzen: head, tail, grep – nie ungefilterte Riesen-Ausgaben.
        - Bei macOS: /usr/bin/log show statt log, /usr/sbin/system_profiler, etc. – immer Vollpfade.
        - Destruktive Aktionen (rm -rf, Disk-Formatierung) nur bei expliziter Bestaetigung.

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
