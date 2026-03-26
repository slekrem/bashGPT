using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: replaces an exact string in a file.
/// Fails when old_string is not found or appears more than once (ambiguous match).
/// </summary>
public sealed class EditFileTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "edit_file",
        Description: """
            Replaces an exact string in a file with a new string.
            The file must exist. old_string must match exactly once — the edit is rejected if it
            is not found or if it matches more than once (add surrounding context to make it unique).
            Use write_file instead when you need to create a new file or rewrite the whole file.
            """,
        Parameters:
        [
            new ToolParameter("path",       "string", "Relative file path inside the repository, e.g. \"src/Foo.cs\".", Required: true),
            new ToolParameter("old_string", "string", "The exact string to find and replace. Must be unique in the file.", Required: true),
            new ToolParameter("new_string", "string", "The replacement string.", Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string path, oldString, newString;
        try
        {
            (path, oldString, newString) = ParseArgs(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}"));
        }

        // Resolve and validate — must stay inside working directory
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Combine(workingDirectory, path));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Invalid path: {ex.Message}"));
        }

        if (!fullPath.StartsWith(workingDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, workingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Path traversal not allowed: '{path}' resolves outside the repository."));
        }

        if (!File.Exists(fullPath))
            return Task.FromResult(new ToolResult(Success: false, Content: $"File not found: '{path}'. Use write_file to create a new file."));

        string original;
        try
        {
            original = File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Read error: {ex.Message}"));
        }

        var count = CountOccurrences(original, oldString);
        if (count == 0)
            return Task.FromResult(new ToolResult(Success: false, Content: $"old_string not found in '{path}'. Make sure it matches the file content exactly (including whitespace and indentation)."));
        if (count > 1)
            return Task.FromResult(new ToolResult(Success: false, Content: $"old_string matches {count} times in '{path}'. Add more surrounding context to make it unique."));

        var updated = original.Replace(oldString, newString, StringComparison.Ordinal);

        try
        {
            File.WriteAllText(fullPath, updated);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Write error: {ex.Message}"));
        }

        return Task.FromResult(new ToolResult(Success: true, Content: $"Edited: {path}"));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static (string path, string oldString, string newString) ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(pathEl.GetString()))
            throw new ArgumentException("missing_required_field: 'path' is required and must be a non-empty string.");

        if (!root.TryGetProperty("old_string", out var oldEl) || oldEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("missing_required_field: 'old_string' is required and must be a string.");

        if (!root.TryGetProperty("new_string", out var newEl) || newEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("missing_required_field: 'new_string' is required and must be a string.");

        if (string.IsNullOrEmpty(oldEl.GetString()))
            throw new ArgumentException("invalid_value: 'old_string' must not be empty.");

        return (pathEl.GetString()!, oldEl.GetString()!, newEl.GetString()!);
    }
}
