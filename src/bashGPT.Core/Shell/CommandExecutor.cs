using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using bashGPT.Core;

namespace BashGPT.Shell;

public record CommandResult(string Command, int ExitCode, string Output, bool WasExecuted);

public class CommandExecutor(
    ExecutionMode mode = ExecutionMode.Ask,
    TextWriter? output = null,
    TextReader? input = null,
    int commandTimeoutSeconds = AppDefaults.CommandTimeoutSeconds)
{
    private static readonly Regex AnsiEscapeRegex =
        new(@"\x1b\[[0-9;]*[mGKHJABCDfnsu]", RegexOptions.Compiled);

    private static readonly Regex InteractiveAlwaysPattern = new(
        @"^\s*(htop|btop|watch|less|more|man|vim|vi|nano|emacs)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TopPattern = new(
        @"^\s*top\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TailFollowPattern = new(
        @"^\s*tail\b.*\s(-f|--follow(?:=[^\s]+)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PingPattern = new(
        @"^\s*ping\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly TextWriter _out = output ?? Console.Out;
    private readonly TextReader _in = input ?? Console.In;

    /// <summary>
    /// Verarbeitet alle extrahierten Befehle: anzeigen, bestaetigen, ausfuehren.
    /// Gibt die Ergebnisse zurueck, auch nicht ausgefuehrte, fuer den LLM-Kontext.
    /// </summary>
    public async Task<IReadOnlyList<CommandResult>> ProcessAsync(
        IReadOnlyList<ExtractedCommand> commands,
        CancellationToken ct = default)
    {
        if (mode == ExecutionMode.NoExec || commands.Count == 0)
            return [];

        var results = new List<CommandResult>();

        foreach (var cmd in commands)
        {
            ct.ThrowIfCancellationRequested();
            var result = await ProcessOneAsync(cmd, ct);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Formatiert die Ergebnisse als Follow-up-Kontext fuer das LLM.
    /// </summary>
    public static string BuildFollowUpContext(IReadOnlyList<CommandResult> results)
    {
        if (results.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## Ausgefuehrte Befehle und Ergebnisse");

        foreach (var r in results)
        {
            if (!r.WasExecuted)
            {
                sb.AppendLine($"- `{r.Command}` -> nicht ausgefuehrt");
                continue;
            }

            sb.AppendLine($"### `{r.Command}` (Exit-Code: {r.ExitCode})");
            if (!string.IsNullOrWhiteSpace(r.Output))
            {
                sb.AppendLine("```");
                sb.AppendLine(r.Output);
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("_(keine Ausgabe)_");
            }
        }

        return sb.ToString();
    }

    private async Task<CommandResult> ProcessOneAsync(ExtractedCommand cmd, CancellationToken ct)
    {
        _out.WriteLine();
        if (cmd.IsDangerous)
        {
            _out.WriteLine($"  WARNING: GEFaeHRLICHER BEFEHL: {cmd.DangerReason}");
            _out.WriteLine($"  ->  {cmd.Command}");
        }
        else
        {
            _out.WriteLine($"  ->  {cmd.Command}");
        }

        if (mode == ExecutionMode.DryRun)
        {
            _out.WriteLine("     (--dry-run: nicht ausgefuehrt)");
            return new CommandResult(cmd.Command, -1, string.Empty, WasExecuted: false);
        }

        if (TryGetInteractiveReason(cmd.Command, out var interactiveReason))
        {
            _out.WriteLine($"     Uebersprungen: {interactiveReason}");
            return new CommandResult(
                cmd.Command,
                -1,
                $"ERROR: {interactiveReason}",
                WasExecuted: false);
        }

        if (mode == ExecutionMode.Ask)
        {
            var prompt = cmd.IsDangerous
                ? "     Trotzdem ausfuehren? [j/N] "
                : "     Ausfuehren? [j/N] ";

            _out.Write(prompt);
            var answer = _in.ReadLine()?.Trim().ToLowerInvariant();

            if (answer is not ("j" or "ja" or "y" or "yes"))
            {
                _out.WriteLine("     Uebersprungen.");
                return new CommandResult(cmd.Command, -1, string.Empty, WasExecuted: false);
            }
        }

        var (exitCode, cmdOutput) = await RunAsync(cmd.Command, ct);
        _out.WriteLine(cmdOutput);
        return new CommandResult(cmd.Command, exitCode, cmdOutput, WasExecuted: true);
    }

    private async Task<(int ExitCode, string Output)> RunAsync(string command, CancellationToken ct)
    {
        var (fileName, arguments) = GetShellArgs(command);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            }
        };

        var sb = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(commandTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Ignore: the process already exited or could not be killed.
            }

            return (-1, $"ERROR: Befehl nach {commandTimeoutSeconds}s abgebrochen.");
        }

        var raw = AnsiEscapeRegex.Replace(sb.ToString(), string.Empty);
        return (process.ExitCode, raw.TrimEnd());
    }

    private static (string FileName, string Arguments) GetShellArgs(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (shell is not null)
                return (shell, $"-c \"{EscapeForUnixShell(command)}\"");
            return ("cmd.exe", $"/c \"{EscapeForWindowsCmd(command)}\"");
        }

        var unixShell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/sh";
        return (unixShell, $"-c \"{EscapeForUnixShell(command)}\"");
    }

    private static string EscapeForUnixShell(string command) =>
        command.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeForWindowsCmd(string command) =>
        command.Replace("\"", "\\\"");

    private static bool TryGetInteractiveReason(string command, out string reason)
    {
        var trimmed = command.Trim();

        if (InteractiveAlwaysPattern.IsMatch(trimmed))
        {
            reason = "Interaktiver Befehl blockiert ohne TTY. Bitte nutze eine nicht-interaktive Alternative.";
            return true;
        }

        if (TopPattern.IsMatch(trimmed) && !IsTopOneShot(trimmed))
        {
            reason = "Interaktiver 'top'-Aufruf erkannt. Nutze z. B. 'top -l 1' (macOS), 'ps aux --sort=-%cpu | head' (Linux) oder 'tasklist' / 'Get-Process' (Windows).";
            return true;
        }

        if (TailFollowPattern.IsMatch(trimmed))
        {
            reason = "Fortlaufender 'tail -f/--follow'-Aufruf erkannt. Bitte ohne Follow-Option ausfuehren.";
            return true;
        }

        var hasPingLimit = trimmed.Contains(" -c ", StringComparison.OrdinalIgnoreCase)
                        || trimmed.Contains(" -n ", StringComparison.OrdinalIgnoreCase);
        if (PingPattern.IsMatch(trimmed) && !hasPingLimit)
        {
            reason = "Dauerlauf bei 'ping' erkannt. Bitte mit Paketlimit ausfuehren, z. B. 'ping -c 4 <host>' (Linux/macOS) oder 'ping -n 4 <host>' (Windows).";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsTopOneShot(string command)
    {
        return Regex.IsMatch(command, @"(^|\s)-l(\s*\d+)?(\s|$)", RegexOptions.IgnoreCase)
            || Regex.IsMatch(command, @"(^|\s)-b(\s|$)", RegexOptions.IgnoreCase)
            || Regex.IsMatch(command, @"(^|\s)-n(\s*\d+)?(\s|$)", RegexOptions.IgnoreCase);
    }
}
