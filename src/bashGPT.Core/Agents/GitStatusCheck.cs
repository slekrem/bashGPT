using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace BashGPT.Agents;

public sealed class GitStatusCheck : IAgentCheck
{
    public AgentCheckType Type => AgentCheckType.GitStatus;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        var workDir = agent.Path ?? Directory.GetCurrentDirectory();

        try
        {
            var output = await RunGitAsync(workDir, ct);
            var hash = ComputeHash(output);
            var changed = hash != agent.LastHash;
            var message = changed
                ? (string.IsNullOrWhiteSpace(output) ? "Repository ist sauber." : $"Änderungen erkannt:\n{output.Trim()}")
                : "Keine Änderungen.";

            return new AgentCheckResult(hash, changed, message, Success: true);
        }
        catch (Exception ex)
        {
            return new AgentCheckResult("error", Changed: true, $"Fehler: {ex.Message}", Success: false);
        }
    }

    private static async Task<string> RunGitAsync(string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git", "status --porcelain")
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git konnte nicht gestartet werden.");
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return output;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
