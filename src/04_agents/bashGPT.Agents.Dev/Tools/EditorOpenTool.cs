using System.Diagnostics;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: opens files matching glob patterns into the Editor.
/// Uses git ls-files internally so .gitignore is respected automatically.
/// </summary>
public sealed class EditorOpenTool : ITool
{
    private const int MaxFileSizeBytes = 131_072; // 128 KB per file

    public ToolDefinition Definition { get; } = new(
        Name: "editor_open",
        Description: "Opens files matching one or more glob patterns into the Editor. Uses git ls-files internally, so .gitignore is respected automatically.",
        Parameters:
        [
            new ToolParameter("patterns", "array", "Glob patterns to match files, e.g. [\"src/**/*.cs\", \"*.sln\"]. Respects .gitignore.", Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string[] patterns;
        try
        {
            patterns = ParsePatterns(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        var sb          = new StringBuilder();
        var loadedPaths = new List<string>();

        foreach (var pattern in patterns)
        {
            var matched = GitLsFiles(pattern);
            if (matched is null) continue;

            foreach (var path in matched.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!File.Exists(path)) continue;

                try
                {
                    var info = new FileInfo(path);
                    if (info.Length > MaxFileSizeBytes)
                    {
                        sb.AppendLine($"## `{path}`\n\n> File too large ({info.Length / 1024} KB), skipped.\n");
                        continue;
                    }

                    sb.Append(EditorState.FormatFileBlock(path, File.ReadAllText(path)));
                    loadedPaths.Add(path);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"## `{path}`\n\n> Error reading file: {ex.Message}\n");
                }
            }
        }

        if (loadedPaths.Count == 0)
            return Task.FromResult(new ToolResult(Success: false, Content: "No files found for the given patterns."));

        // Persist paths in the session so DevAgent.SystemPrompt picks them up on every request.
        EditorState.AddFiles(loadedPaths, call.SessionPath);

        return Task.FromResult(new ToolResult(
            Success: true,
            Content: $"{loadedPaths.Count} file(s) opened in Editor: {string.Join(", ", loadedPaths)}"));
    }

    private static string[] ParsePatterns(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("patterns", out var patternsEl))
            throw new ArgumentException("missing_required_field: 'patterns' is required. Example: {\"patterns\":[\"src/**/*.cs\"]}");

        if (patternsEl.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in patternsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    throw new ArgumentException("invalid_type: each entry in 'patterns' must be a string.");
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    list.Add(s);
            }
            if (list.Count == 0)
                throw new ArgumentException("invalid_value: 'patterns' must contain at least one non-empty string.");
            return [.. list];
        }

        // Fallback: single pattern string
        if (patternsEl.ValueKind == JsonValueKind.String)
        {
            var s = patternsEl.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("invalid_value: 'patterns' must not be empty.");
            return [s];
        }

        throw new ArgumentException("invalid_type: 'patterns' must be an array of strings.");
    }

    private static string? GitLsFiles(string pattern)
    {
        // Tracked files + untracked files (respects .gitignore via --exclude-standard)
        var tracked   = RunGit($"ls-files \"{pattern}\"");
        var untracked = RunGit($"ls-files --others --exclude-standard \"{pattern}\"");
        var combined  = string.Join('\n', new[] { tracked, untracked }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static string? RunGit(string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            return proc?.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch { return null; }
    }
}
