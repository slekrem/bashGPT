using System.Diagnostics;
using System.Text;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.GitHub;

internal static class GhRunner
{
    internal static async Task<(string Stdout, string Stderr, int ExitCode)> RunAsync(
        IEnumerable<string> args, string? cwd, CancellationToken ct, string executable = "gh")
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (cwd is not null)
            psi.WorkingDirectory = cwd;
        foreach (var a in args)
            psi.ArgumentList.Add(a);

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
                "Install GitHub CLI (https://cli.github.com) and run 'gh auth login'.",
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
