using BashGPT.Configuration;
using BashGPT.Shell;
using BashGPT.Storage;

namespace BashGPT;

public static class AppBootstrap
{
    public static string GetConfigDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "bashgpt");

    public static ProviderType? ParseProviderOrThrow(string? providerStr) => providerStr?.ToLowerInvariant() switch
    {
        "ollama" => ProviderType.Ollama,
        "cerebras" => ProviderType.Cerebras,
        null => null,
        var value => throw new ArgumentException($"Unbekannter Provider '{value}'. Erlaubt: ollama, cerebras")
    };

    public static ExecutionMode ResolveExecutionMode(bool noExec, bool dryRun, bool autoExec) => (noExec, dryRun, autoExec) switch
    {
        (true, _, _) => ExecutionMode.NoExec,
        (_, true, _) => ExecutionMode.DryRun,
        (_, _, true) => ExecutionMode.AutoExec,
        _ => ExecutionMode.Ask,
    };

    public static SessionStore CreateSessionStore(string? configDir = null)
    {
        var baseDir = configDir ?? GetConfigDir();
        var historyFile = Path.Combine(baseDir, "history.json");
        var sessionsFile = Path.Combine(baseDir, "sessions.json");
        return new SessionStore(sessionsFile, legacyHistoryFile: historyFile);
    }
}
