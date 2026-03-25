using System.Text;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Manages the list of files currently open in the Editor.
/// Stored as "editor-files" in the session directory.
/// The session path is passed explicitly via the <c>sessionPath</c> parameter on each method.
/// </summary>
public static class EditorState
{
    private const string CacheFileName = "editor-files";

    public static string GetCachePath(string? sessionPath = null)
    {
        var dir = !string.IsNullOrWhiteSpace(sessionPath) ? sessionPath
                : Directory.GetCurrentDirectory();
        return Path.Combine(dir, CacheFileName);
    }

    /// <summary>Adds new paths to the editor, ignoring duplicates.</summary>
    public static void AddFiles(IEnumerable<string> paths, string? sessionPath = null)
    {
        var existing = ReadFiles(sessionPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newPaths = paths.Where(p => existing.Add(p)).ToList();
        if (newPaths.Count == 0) return;
        var cachePath = GetCachePath(sessionPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.AppendAllLines(cachePath, newPaths);
    }

    /// <summary>Returns all currently open file paths.</summary>
    public static IReadOnlyList<string> ReadFiles(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (!File.Exists(cachePath)) return [];
        return File.ReadAllLines(cachePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Removes specific paths from the editor.</summary>
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
        // Drop trailing empty element produced by a final newline
        if (lines.Length > 0 && string.IsNullOrEmpty(lines[^1].TrimEnd()))
            lines = lines[..^1];
        var width = lines.Length.ToString().Length;
        var sb    = new StringBuilder();
        sb.AppendLine($"## `{path}`\n\n```{ext}");
        for (var i = 0; i < lines.Length; i++)
            sb.AppendLine($"{(i + 1).ToString().PadLeft(width)}\t{lines[i].TrimEnd()}");
        sb.AppendLine("```\n");
        return sb.ToString();
    }

    /// <summary>Closes all open files.</summary>
    public static void Clear(string? sessionPath = null)
    {
        var cachePath = GetCachePath(sessionPath);
        if (File.Exists(cachePath)) File.Delete(cachePath);
    }
}
