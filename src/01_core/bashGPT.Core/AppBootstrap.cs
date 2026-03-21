using System.Runtime.InteropServices;
using bashGPT.Core.Storage;

namespace bashGPT.Core;

public static class AppBootstrap
{
    public static string GetDefaultConfigDir() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "bashgpt")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                "bashgpt");

    public static string GetDefaultConfigFilePath() =>
        Path.Combine(GetDefaultConfigDir(), "config.json");

    public static string GetPluginsDir(string? configDir = null) =>
        Path.Combine(configDir ?? GetDefaultConfigDir(), "plugins");

    /// <summary>
    /// Returns the plugins directory bundled alongside the application binary.
    /// Bundled plugins are shipped with the app and loaded automatically at startup.
    /// </summary>
    public static string GetBundledPluginsDir() =>
        Path.Combine(AppContext.BaseDirectory, "plugins");

    public static string GetSessionsDir(string? configDir = null) =>
        Path.Combine(configDir ?? GetDefaultConfigDir(), "sessions");

    public static SessionStore CreateSessionStore(string? configDir = null) =>
        new(GetSessionsDir(configDir));

    public static SessionRequestStore CreateSessionRequestStore(string? configDir = null) =>
        new(GetSessionsDir(configDir));
}
