using System.Diagnostics;
using System.Text;

namespace BashGPT.Shell;

public record CommandResult(string Command, int ExitCode, string Output, bool WasExecuted);

public class CommandExecutor(
    ExecutionMode mode = ExecutionMode.Ask,
    TextWriter? output = null,
    TextReader? input = null,
    int maxOutputChars = 10_000)
{
    private readonly TextWriter _out   = output ?? Console.Out;
    private readonly TextReader _in    = input  ?? Console.In;

    /// <summary>
    /// Verarbeitet alle extrahierten Befehle: anzeigen, bestätigen, ausführen.
    /// Gibt die Ergebnisse zurück – auch nicht ausgeführte, für den LLM-Kontext.
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
    /// Formatiert die Ergebnisse als Follow-up-Kontext für das LLM.
    /// </summary>
    public static string BuildFollowUpContext(IReadOnlyList<CommandResult> results)
    {
        if (results.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## Ausgeführte Befehle und Ergebnisse");

        foreach (var r in results)
        {
            if (!r.WasExecuted)
            {
                sb.AppendLine($"- `{r.Command}` → nicht ausgeführt");
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

    // ── Intern ──────────────────────────────────────────────────────────────

    private async Task<CommandResult> ProcessOneAsync(ExtractedCommand cmd, CancellationToken ct)
    {
        // Befehl anzeigen
        _out.WriteLine();
        if (cmd.IsDangerous)
        {
            _out.WriteLine($"  ⚠  GEFÄHRLICHER BEFEHL: {cmd.DangerReason}");
            _out.WriteLine($"  →  {cmd.Command}");
        }
        else
        {
            _out.WriteLine($"  →  {cmd.Command}");
        }

        if (mode == ExecutionMode.DryRun)
        {
            _out.WriteLine("     (--dry-run: nicht ausgeführt)");
            return new CommandResult(cmd.Command, -1, string.Empty, WasExecuted: false);
        }

        // Bestätigung einholen (außer bei AutoExec)
        if (mode == ExecutionMode.Ask)
        {
            var prompt = cmd.IsDangerous
                ? "     Trotzdem ausführen? [j/N] "
                : "     Ausführen? [j/N] ";

            _out.Write(prompt);
            var answer = _in.ReadLine()?.Trim().ToLowerInvariant();

            if (answer is not ("j" or "ja" or "y" or "yes"))
            {
                _out.WriteLine("     Übersprungen.");
                return new CommandResult(cmd.Command, -1, string.Empty, WasExecuted: false);
            }
        }

        // Ausführen
        var (exitCode, cmdOutput) = await RunAsync(cmd.Command, ct);
        _out.WriteLine(cmdOutput);
        return new CommandResult(cmd.Command, exitCode, cmdOutput, WasExecuted: true);
    }

    private async Task<(int ExitCode, string Output)> RunAsync(string command, CancellationToken ct)
    {
        var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/sh";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = shell,
                Arguments              = $"-c \"{EscapeForShell(command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                WorkingDirectory       = Directory.GetCurrentDirectory()
            }
        };

        var sb = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        var raw = sb.ToString();
        var truncated = raw.Length > maxOutputChars
            ? raw[..maxOutputChars] + $"\n… (auf {maxOutputChars} Zeichen gekürzt)"
            : raw;

        return (process.ExitCode, truncated.TrimEnd());
    }

    private static string EscapeForShell(string command) =>
        command.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
