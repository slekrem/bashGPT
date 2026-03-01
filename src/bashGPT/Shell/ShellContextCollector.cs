using System.Runtime.InteropServices;

namespace BashGPT.Shell;

public class ShellContextCollector
{
    // Umgebungsvariablen, die nützlich sind, aber keine Secrets
    private static readonly HashSet<string> AllowedEnvVars = new(StringComparer.OrdinalIgnoreCase)
    {
        "PATH", "HOME", "USER", "LOGNAME", "LANG", "LC_ALL", "TERM",
        "EDITOR", "VISUAL", "PAGER", "COLORTERM", "COLUMNS", "LINES"
    };

    public async Task<ShellContext> CollectAsync(bool includeDirectoryListing = false)
    {
        var pwd    = Directory.GetCurrentDirectory();
        var os     = GetOsDescription();
        var shell  = System.Environment.GetEnvironmentVariable("SHELL") ?? "(unbekannt)";
        var git    = await TryCollectGitContextAsync(pwd);
        var dir    = includeDirectoryListing ? GetDirectoryEntries(pwd) : [];
        var env    = GetFilteredEnvironment();

        return new ShellContext(pwd, os, shell, git, dir, env);
    }

    public string BuildSystemPrompt(ShellContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Du bist ein kompetenter Shell-Assistent, der Aufgaben direkt auf dem System des Nutzers ausführt.");
        sb.AppendLine("Antworte auf Deutsch, es sei denn, der Nutzer schreibt in einer anderen Sprache.");
        sb.AppendLine();
        sb.AppendLine("## Fähigkeiten");
        sb.AppendLine("Du hast Zugriff auf das `bash`-Tool, mit dem du Shell-Befehle direkt ausführen kannst.");
        sb.AppendLine("Du bist ein Executor – führe Aufgaben aktiv aus, statt sie nur zu erklären oder vorzuschlagen.");
        sb.AppendLine();
        sb.AppendLine("## Aktueller Kontext");
        sb.AppendLine($"- **Verzeichnis:** `{ctx.WorkingDirectory}`");
        sb.AppendLine($"- **OS:** {ctx.OperatingSystem}");
        sb.AppendLine($"- **Shell:** {ctx.Shell}");

        if (ctx.Git is { } git)
        {
            sb.AppendLine($"- **Git-Branch:** `{git.Branch}`");
            if (git.LastCommit is not null)
                sb.AppendLine($"- **Letzter Commit:** {git.LastCommit}");
            if (git.ChangedFiles.Count > 0)
            {
                sb.AppendLine($"- **Geänderte Dateien ({git.ChangedFiles.Count}):**");
                foreach (var f in git.ChangedFiles.Take(10))
                    sb.AppendLine($"  - `{f}`");
                if (git.ChangedFiles.Count > 10)
                    sb.AppendLine($"  - … und {git.ChangedFiles.Count - 10} weitere");
            }
        }
        else
        {
            sb.AppendLine("- **Git:** Kein Git-Repository");
        }

        if (ctx.DirectoryEntries.Count > 0)
        {
            sb.AppendLine("- **Verzeichnisinhalt:**");
            foreach (var entry in ctx.DirectoryEntries.Take(20))
                sb.AppendLine($"  - `{entry}`");
        }

        sb.AppendLine();
        sb.AppendLine("## Regeln");
        sb.AppendLine("- Nutze das bash-Tool aktiv, um Befehle auszuführen – erkläre kurz, was du tust.");
        sb.AppendLine("- Verwende ausschließlich nicht-interaktive Befehle ohne TTY-Anforderung.");
        sb.AppendLine("  Verboten: `top`, `htop`, `btop`, `vim`, `nano`, `less`, `more`, `watch`, `tail -f`.");
        sb.AppendLine("- Halte Ausgaben kurz: filtere mit `head -n 50`, `tail`, `grep` oder ähnlichem.");
        sb.AppendLine("- Nutze Vollpfade für Systembefehle, wenn ein Namenskonflikt möglich ist (z. B. `/usr/bin/log` statt `log`).");
        sb.AppendLine("- Analysiere Fehlerausgaben und versuche bei Bedarf eine alternative Vorgehensweise.");
        sb.AppendLine("- Führe keine destruktiven Aktionen (rm -rf, Disk-Formatierung etc.) ohne explizite Bestätigung aus.");

        return sb.ToString();
    }

    // ── Interne Hilfsmethoden ────────────────────────────────────────────────

    private static string GetOsDescription()
    {
        var desc = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))   return $"macOS ({arch}) – {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))  return $"Linux ({arch}) – {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"Windows ({arch}) – {desc}";
        return $"{desc} ({arch})";
    }

    private static async Task<GitContext?> TryCollectGitContextAsync(string pwd)
    {
        // Prüfen ob wir in einem Git-Repo sind
        if (!IsGitRepo(pwd)) return null;

        var branch     = await RunGitAsync("rev-parse --abbrev-ref HEAD", pwd);
        var lastCommit = await RunGitAsync("log -1 --pretty=format:\"%h %s\"", pwd);
        var statusOut  = await RunGitAsync("status --porcelain", pwd);

        if (branch is null) return null;

        var changed = statusOut is null
            ? []
            : statusOut.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimStart())
                .Where(l => l.Length > 0)
                .ToList();

        return new GitContext(branch, lastCommit, changed);
    }

    private static bool IsGitRepo(string path)
    {
        var dir = new DirectoryInfo(path);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return true;
            dir = dir.Parent;
        }
        return false;
    }

    private static async Task<string?> RunGitAsync(string args, string workingDir)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = "git",
                    Arguments              = args,
                    WorkingDirectory       = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> GetDirectoryEntries(string path)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(path)
                .Select(e => Path.GetFileName(e) + (Directory.Exists(e) ? "/" : ""))
                .OrderBy(e => e)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyDictionary<string, string> GetFilteredEnvironment()
    {
        var result = new Dictionary<string, string>();
        foreach (System.Collections.DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key is not null && AllowedEnvVars.Contains(key) && entry.Value?.ToString() is { } val)
                result[key] = val;
        }
        return result;
    }
}
