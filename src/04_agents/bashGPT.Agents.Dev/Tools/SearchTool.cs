using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: searches for a text string across files in the repository.
/// </summary>
public sealed class SearchTool(string workingDirectory) : ITool
{
    private const int DefaultMaxResults = 20;

    private static readonly string[] IgnoredDirectories =
        [".git", "obj", "bin", "node_modules", ".vs"];

    public ToolDefinition Definition { get; } = new(
        Name: "search",
        Description: "Searches for a text string inside files in the repository. Returns matching lines with file path and line number.",
        Parameters:
        [
            new ToolParameter("query",       "string",  "Text to search for (case-insensitive).",                                              Required: true),
            new ToolParameter("path",        "string",  "Directory or file path to search in (relative). Defaults to the repository root.",    Required: false),
            new ToolParameter("max_results", "integer", $"Maximum number of matching lines to return. Defaults to {DefaultMaxResults}.",        Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string query;
        string searchPath;
        int maxResults;
        try
        {
            (query, searchPath, maxResults) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        // Resolve and validate search root
        string fullSearchPath;
        try
        {
            fullSearchPath = Path.GetFullPath(Path.Combine(workingDirectory, searchPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid path: {ex.Message}"));
        }

        if (!fullSearchPath.StartsWith(workingDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullSearchPath, workingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Path traversal not allowed: '{searchPath}' resolves outside the repository."));
        }

        if (!Directory.Exists(fullSearchPath) && !File.Exists(fullSearchPath))
            return Task.FromResult(new ToolResult(Success: false, Content: $"Path not found: '{searchPath}'."));

        var matches = new List<string>();
        try
        {
            var files = File.Exists(fullSearchPath)
                ? [fullSearchPath]
                : EnumerateFiles(fullSearchPath);

            foreach (var file in files)
            {
                if (matches.Count >= maxResults) break;
                ct.ThrowIfCancellationRequested();
                SearchFile(file, query, workingDirectory, maxResults, matches);
            }
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: "Search cancelled."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Search error: {ex.Message}"));
        }

        if (matches.Count == 0)
            return Task.FromResult(new ToolResult(Success: true, Content: $"No matches found for '{query}'."));

        var sb = new StringBuilder();
        sb.AppendLine($"{matches.Count} match(es) for '{query}':{(matches.Count >= maxResults ? $" (limited to {maxResults})" : "")}\n");
        foreach (var m in matches)
            sb.AppendLine(m);

        return Task.FromResult(new ToolResult(Success: true, Content: sb.ToString().TrimEnd()));
    }

    private static void SearchFile(string file, string query, string workingDirectory, int maxResults, List<string> matches)
    {
        var relative = Path.GetRelativePath(workingDirectory, file);
        string[] lines;
        try { lines = File.ReadAllLines(file); }
        catch { return; }

        for (var i = 0; i < lines.Length && matches.Count < maxResults; i++)
        {
            if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                matches.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
        }
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var queue = new Queue<string>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var dir = queue.Dequeue();
            string[] entries;
            try { entries = Directory.GetFileSystemEntries(dir); }
            catch { continue; }

            foreach (var entry in entries.Order())
            {
                if (Directory.Exists(entry))
                {
                    if (!IgnoredDirectories.Contains(Path.GetFileName(entry), StringComparer.OrdinalIgnoreCase))
                        queue.Enqueue(entry);
                }
                else
                {
                    yield return entry;
                }
            }
        }
    }

    private static (string query, string path, int maxResults) ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("query", out var queryEl) || queryEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(queryEl.GetString()))
            throw new ArgumentException("missing_required_field: 'query' is required and must be a non-empty string.");

        var path = root.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String
            ? pathEl.GetString() ?? "."
            : ".";

        var maxResults = DefaultMaxResults;
        if (root.TryGetProperty("max_results", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number
            && maxEl.TryGetInt32(out var m) && m > 0)
            maxResults = m;

        return (queryEl.GetString()!, path, maxResults);
    }
}
