using System.Diagnostics;
using System.Text;

namespace BashGPT.Tools.Git;

internal static class GitRunner
{
    internal static async Task<(string Stdout, string Stderr, int ExitCode)> RunAsync(
        string args, string? cwd, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git", args)
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

        process.Start();

        var outTask = process.StandardOutput.ReadToEndAsync(ct);
        var errTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        stdout.Append(await outTask);
        stderr.Append(await errTask);

        return (stdout.ToString(), stderr.ToString(), process.ExitCode);
    }
}
