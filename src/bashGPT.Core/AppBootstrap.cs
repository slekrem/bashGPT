using bashGPT.Core.Storage;

namespace bashGPT.Core;

public static class AppBootstrap
{
    private static string GetConfigDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "bashgpt");

    public static SessionStore CreateSessionStore(string? configDir = null)
    {
        var baseDir = configDir ?? GetConfigDir();
        var sessionsDir = Path.Combine(baseDir, "sessions");
        return new SessionStore(sessionsDir);
    }
}
