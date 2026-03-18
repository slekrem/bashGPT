using bashGPT.Core.Storage;

namespace bashGPT.Core;

public static class AppBootstrap
{
    private static string GetConfigDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "bashgpt");

    public static SessionStore CreateSessionStore(string? configDir = null)
    {
        var baseDir = configDir ?? GetConfigDir();
        var historyFile = Path.Combine(baseDir, "history.json");
        var legacySessionsFile = Path.Combine(baseDir, "sessions.json");
        var sessionsDir = Path.Combine(baseDir, "sessions");
        return new SessionStore(sessionsDir, legacyHistoryFile: historyFile, legacySessionsFile: legacySessionsFile);
    }
}
