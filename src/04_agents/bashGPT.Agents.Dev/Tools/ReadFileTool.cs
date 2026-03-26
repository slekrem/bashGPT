using System.Diagnostics;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: reads one or more files and returns their content.
/// Only accepts paths that are visible in the File Explorer (git-tracked or untracked, not gitignored).
/// </summary>
public sealed class ReadFileTool(string workingDirectory) : ITool
{
    private const int MaxFileSizeBytes = 131_072; // 128 KB per file

    public ToolDefinition Definition { get; } = new(
        Name: "read_file",
        Description: "Reads files from the File Explorer and returns their current content. Only paths visible in the File Explorer are accepted — no external or gitignored files.",
        Parameters:
        [
            new ToolParameter("paths", "array", "Exact file paths from the File Explorer, e.g. [\"src/Foo.cs\", \"src/Bar.cs\"].", Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string[] paths;
        try
        {
            paths = ParsePaths(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var projectFiles = GetProjectFiles();
        var sb       = new StringBuilder();
        var rejected = new List<string>();

        foreach (var path in paths)
        {
            if (!projectFiles.Contains(path))
            {
                rejected.Add(path);
                continue;
            }

            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxFileSizeBytes)
                {
                    sb.AppendLine($"> `{path}` — file too large ({info.Length / 1024} KB), skipped.");
                    sb.AppendLine();
                    continue;
                }
                sb.AppendLine(FormatFileBlock(path, File.ReadAllText(path)).TrimEnd());
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"> `{path}` — read error: {ex.Message}");
                sb.AppendLine();
            }
        }

        if (sb.Length == 0)
        {
            var reasons = rejected.Count > 0
                ? $"not in File Explorer: {string.Join(", ", rejected)}"
                : "no files could be read";
            return Task.FromResult(new ToolResult(Success: false, Content: $"No files read. {reasons}"));
        }

        if (rejected.Count > 0)
            sb.AppendLine($"Not in File Explorer (skipped): {string.Join(", ", rejected)}");

        return Task.FromResult(new ToolResult(Success: true, Content: sb.ToString().TrimEnd()));
    }

    private HashSet<string> GetProjectFiles()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in RunGit("ls-files"))
            result.Add(line);
        foreach (var line in RunGit("ls-files --others --exclude-standard"))
            result.Add(line);
        return result;
    }

    private IEnumerable<string> RunGit(string args)
    {
        string? output = null;
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("git", args)
            {
                WorkingDirectory       = workingDirectory,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            if (proc?.ExitCode != 0) output = null;
        }
        catch { /* git not available */ }

        if (string.IsNullOrWhiteSpace(output)) yield break;
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            yield return line.Trim();
    }

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

    private static string[] ParsePaths(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("paths", out var pathsEl))
            throw new ArgumentException("missing_required_field: 'paths' is required. Example: {\"paths\":[\"src/Foo.cs\"]}");

        if (pathsEl.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("invalid_type: 'paths' must be an array of strings.");

        var list = new List<string>();
        foreach (var item in pathsEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw new ArgumentException("invalid_type: each entry in 'paths' must be a string.");
            var s = item.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                list.Add(s);
        }
        if (list.Count == 0)
            throw new ArgumentException("invalid_value: 'paths' must contain at least one non-empty string.");
        return [.. list];
    }
}
