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

    public static string GetSessionsDir(string? configDir = null) =>
        Path.Combine(configDir ?? GetDefaultConfigDir(), "sessions");

    public static SessionStore CreateSessionStore(string? configDir = null) =>
        new(GetSessionsDir(configDir));

    public static SessionRequestStore CreateSessionRequestStore(string? configDir = null) =>
        new(GetSessionsDir(configDir));
}
