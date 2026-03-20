using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev;

/// <summary>
/// Built-in dev agent tool: removes file paths from the session-scoped context cache.
/// </summary>
public sealed class ContextUnloadFilesTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "context_unload_files",
        Description: "Removes previously loaded files from the context. Accepts exact paths or glob patterns.",
        Parameters:
        [
            new ToolParameter("patterns", "array", "Exact file paths or glob patterns to remove, e.g. [\"src/Foo.cs\", \"tests/**/*.cs\"].", Required: true),
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

        var cached  = ContextFileCache.ReadFiles(call.SessionPath);
        var matched = cached
            .Where(p => patterns.Any(pat => MatchesPattern(p, pat)))
            .ToList();

        if (matched.Count == 0)
            return Task.FromResult(new ToolResult(Success: false,
                Content: "None of the specified files were in the context."));

        ContextFileCache.RemoveFiles(matched, call.SessionPath);

        return Task.FromResult(new ToolResult(
            Success: true,
            Content: $"{matched.Count} file(s) removed from context: {string.Join(", ", matched)}"));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Exact match
        if (string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Simple glob: only ** and * are supported
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/\\\\]*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(
            path.Replace('\\', '/'),
            regex.Replace('\\', '/'),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string[] ParsePatterns(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("patterns", out var patternsEl))
            throw new ArgumentException("missing_required_field: 'patterns' is required. Example: {\"patterns\":[\"src/Foo.cs\"]}");

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

        if (patternsEl.ValueKind == JsonValueKind.String)
        {
            var s = patternsEl.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("invalid_value: 'patterns' must not be empty.");
            return [s];
        }

        throw new ArgumentException("invalid_type: 'patterns' must be an array of strings.");
    }
}
