using System.Text.RegularExpressions;

namespace bashGPT.Core.Storage;

internal static class SessionStoragePaths
{
    private static readonly Regex ValidIdPattern = new(@"^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled);

    public static void ValidateSessionId(string sessionsDir, string id)
    {
        if (!ValidIdPattern.IsMatch(id))
            throw new ArgumentException($"Invalid session ID: '{id}'.", nameof(id));

        var root = Path.GetFullPath(sessionsDir) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(sessionsDir, id)) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Session ID '{id}' resolves outside the allowed directory.", nameof(id));
    }

    public static string GetSessionDir(string sessionsDir, string sessionId) => Path.Combine(sessionsDir, sessionId);

    public static string GetContentFilePath(string sessionsDir, string sessionId) =>
        Path.Combine(sessionsDir, sessionId, "content.json");

    public static string GetRequestsDir(string sessionsDir, string sessionId) =>
        Path.Combine(GetSessionDir(sessionsDir, sessionId), "requests");

    public static string GetSafeTimestamp(string timestamp) =>
        timestamp.Replace(":", "-").Replace("+", "+");
}
