namespace BashGPT.Agents.Dev;

/// <summary>
/// Verwaltet eine persistente Liste von Dateipfaden, die der Dev-Agent als Kontext laden soll.
/// Gespeichert als "context-files" im Session-Ordner.
/// Der aktuelle Session-Pfad wird über <see cref="CurrentSessionPath"/> per AsyncLocal gesetzt,
/// sodass <see cref="DevAgent.SystemPrompt"/> ihn beim Aufbau des System-Prompts kennt.
/// </summary>
public static class ContextFileCache
{
    private const string CacheFileName = "context-files";

    private static readonly AsyncLocal<string?> _currentSessionPath = new();

    /// <summary>
    /// Muss vom Server-Handler gesetzt werden, bevor <c>agent.SystemPrompt</c> ausgewertet wird.
    /// </summary>
    public static string? CurrentSessionPath
    {
        get => _currentSessionPath.Value;
        set => _currentSessionPath.Value = value;
    }

    public static string GetCachePath(string? sessionPath = null)
    {
        var dir = !string.IsNullOrWhiteSpace(sessionPath) ? sessionPath
                : !string.IsNullOrWhiteSpace(CurrentSessionPath) ? CurrentSessionPath
                : Directory.GetCurrentDirectory();
        return Path.Combine(dir, CacheFileName);
    }

    /// <summary>Fügt neue Pfade hinzu (Duplikate werden ignoriert).</summary>
    public static void AddFiles(IEnumerable<string> paths, string? sessionPath = null)
    {
        var existing = ReadFiles(sessionPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newPaths = paths.Where(p => existing.Add(p)).ToList();
        if (newPaths.Count == 0) return;
        var cachePath = GetCachePath(sessionPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.AppendAllLines(cachePath, newPaths);
    }

    /// <summary>Gibt alle gespeicherten Dateipfade zurück.</summary>
    public static IReadOnlyList<string> ReadFiles(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (!File.Exists(cachePath)) return [];
        return File.ReadAllLines(cachePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Entfernt bestimmte Pfade aus dem Cache.</summary>
    public static void RemoveFiles(IEnumerable<string> paths, string? sessionPath = null)
    {
        var toRemove = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remaining = ReadFiles(sessionPath).Where(p => !toRemove.Contains(p)).ToList();
        var cachePath = GetCachePath(sessionPath);
        if (remaining.Count == 0)
        {
            if (File.Exists(cachePath)) File.Delete(cachePath);
            return;
        }
        File.WriteAllLines(cachePath, remaining);
    }

    /// <summary>Löscht den Cache.</summary>
    public static void Clear(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (File.Exists(cachePath)) File.Delete(cachePath);
    }
}
