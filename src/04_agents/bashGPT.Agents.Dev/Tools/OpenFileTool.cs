using System.Diagnostics;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: opens one or more files into the Editor.
/// Only accepts paths that are visible in the File Explorer (git-tracked or untracked, not gitignored).
/// </summary>
public sealed class OpenFileTool(string workingDirectory) : ITool
{
    private const int MaxFileSizeBytes = 131_072; // 128 KB per file

    public ToolDefinition Definition { get; } = new(
        Name: "open_file",
        Description: "Opens files from the File Explorer into the Editor. Only paths visible in the File Explorer are accepted — no external or gitignored files. File content will appear in the Editor section on the next message, not in this tool result.",
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
        var opened   = new List<string>();
        var rejected = new List<string>();
        var skipped  = new List<string>();

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
                if (info.Length > MaxFileSizeBytes) { skipped.Add($"{path} (too large, {info.Length / 1024} KB)"); continue; }
                opened.Add(path);
            }
            catch (Exception ex) { skipped.Add($"{path} ({ex.Message})"); }
        }

        if (opened.Count == 0)
        {
            var reasons = new List<string>();
            if (rejected.Count > 0) reasons.Add($"not in File Explorer: {string.Join(", ", rejected)}");
            if (skipped.Count  > 0) reasons.Add($"unreadable: {string.Join(", ", skipped)}");
            return Task.FromResult(new ToolResult(Success: false, Content: $"No files opened. {string.Join("; ", reasons)}"));
        }

        EditorState.AddFiles(opened, call.SessionPath);

        var msg = $"Opened in Editor: {string.Join(", ", opened)}";
        if (rejected.Count > 0) msg += $"\nNot in File Explorer: {string.Join(", ", rejected)}";
        if (skipped.Count  > 0) msg += $"\nSkipped: {string.Join(", ", skipped)}";
        return Task.FromResult(new ToolResult(Success: true, Content: msg));
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
