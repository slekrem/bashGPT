using System.Diagnostics;
using System.Text;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Git;

internal static class GitRunner
{
    internal static async Task<(string Stdout, string Stderr, int ExitCode)> RunAsync(
        string args, string? cwd, CancellationToken ct, string executable = "git")
    {
        var psi = new ProcessStartInfo(executable, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (cwd is not null)
            psi.WorkingDirectory = cwd;

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            var missingExecutable = ExternalDependencyErrors.TryCreateMissingExecutableException(
                executable,
                "Install Git and verify 'git --version' works in your shell.",
                ex);

            if (missingExecutable is not null)
                return (string.Empty, missingExecutable.Message, -1);

            throw;
        }

        var outTask = process.StandardOutput.ReadToEndAsync(ct);
        var errTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        stdout.Append(await outTask);
        stderr.Append(await errTask);

        return (stdout.ToString(), stderr.ToString(), process.ExitCode);
    }
}
