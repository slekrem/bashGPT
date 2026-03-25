using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: closes one or more open files from the Editor.
/// Pass an empty list to close all open files.
/// </summary>
public sealed class CloseFileTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "close_file",
        Description: "Closes one or more open files from the Editor. Closed files will no longer appear in the Editor section. Pass an empty list [] to close all open files at once.",
        Parameters:
        [
            new ToolParameter("paths", "array", "List of file paths to close. Pass [] to close all open files.", Required: true),
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

        // Empty list = close all
        if (paths.Length == 0)
        {
            var total = EditorState.ReadFiles(call.SessionPath).Count;
            EditorState.Clear(call.SessionPath);
            return Task.FromResult(new ToolResult(Success: true,
                Content: total > 0 ? $"All {total} file(s) closed." : "Editor was already empty."));
        }

        var open    = EditorState.ReadFiles(call.SessionPath);
        var matched = open
            .Where(p => paths.Any(req => string.Equals(p, req, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matched.Count == 0)
            return Task.FromResult(new ToolResult(Success: false,
                Content: "None of the specified files were open in the Editor."));

        EditorState.RemoveFiles(matched, call.SessionPath);

        return Task.FromResult(new ToolResult(Success: true,
            Content: $"{matched.Count} file(s) closed: {string.Join(", ", matched)}"));
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
        return [.. list];
    }
}
