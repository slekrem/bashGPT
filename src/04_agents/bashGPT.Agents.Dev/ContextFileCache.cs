using System.Text;

namespace bashGPT.Agents.Dev;

/// <summary>
/// Manages a persistent list of file paths that the dev agent loads into context.
/// Stored as "context-files" in the session directory.
/// The current session path is provided via <see cref="CurrentSessionPath"/> using AsyncLocal,
/// so <see cref="DevAgent.SystemPrompt"/> can access it when building the system prompt.
/// </summary>
public static class ContextFileCache
{
    private const string CacheFileName = "context-files";

    private static readonly AsyncLocal<string?> _currentSessionPath = new();

    /// <summary>
    /// Must be set by the server handler before <c>agent.SystemPrompt</c> is evaluated.
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

    /// <summary>Adds new paths to the cache, ignoring duplicates.</summary>
    public static void AddFiles(IEnumerable<string> paths, string? sessionPath = null)
    {
        var existing = ReadFiles(sessionPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newPaths = paths.Where(p => existing.Add(p)).ToList();
        if (newPaths.Count == 0) return;
        var cachePath = GetCachePath(sessionPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.AppendAllLines(cachePath, newPaths);
    }

    /// <summary>Returns all stored file paths.</summary>
    public static IReadOnlyList<string> ReadFiles(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (!File.Exists(cachePath)) return [];
        return File.ReadAllLines(cachePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Removes specific paths from the cache.</summary>
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

    /// <summary>
    /// Returns the content of a file as a formatted markdown block
    /// including right-aligned, tab-separated line numbers.
    /// </summary>
    internal static string FormatFileBlock(string path, string content)
    {
        var ext   = Path.GetExtension(path).TrimStart('.');
        var lines = content.Split('\n');
        var width = lines.Length.ToString().Length;
        var sb    = new StringBuilder();
        sb.AppendLine($"## `{path}`\n\n```{ext}");
        for (var i = 0; i < lines.Length; i++)
            sb.AppendLine($"{(i + 1).ToString().PadLeft(width)}\t{lines[i].TrimEnd()}");
        sb.AppendLine("```\n");
        return sb.ToString();
    }

    /// <summary>Deletes the cache file.</summary>
    public static void Clear(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (File.Exists(cachePath)) File.Delete(cachePath);
    }
}
