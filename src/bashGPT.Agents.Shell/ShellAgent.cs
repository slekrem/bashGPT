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
        Du bist ein erfahrener Shell-Assistent. Nutze das shell_exec-Tool, um Befehle auszufuehren
        und dem Benutzer bei Terminal-Aufgaben zu helfen. Erklaere, was du tust, und zeige
        Ergebnisse uebersichtlich.

        Verwende ausschliesslich nicht-interaktive Befehle ohne TTY-Anforderung.
        Verboten: top, htop, vim, nano, less, more, watch, tail -f.
        Halte Ausgaben kurz: filtere mit head, tail, grep oder aehnlichem.
        Fuehre keine destruktiven Aktionen (rm -rf, Disk-Formatierung) ohne explizite Bestaetigung aus.

        ## Systemkontext
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
