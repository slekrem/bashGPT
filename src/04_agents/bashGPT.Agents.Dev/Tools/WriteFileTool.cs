using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: writes the complete content of a single file.
/// The path must be inside the working directory — no path traversal allowed.
/// </summary>
public sealed class WriteFileTool(string workingDirectory) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "write_file",
        Description: "Writes the complete content to a file inside the repository. Creates the file if it does not exist, overwrites it otherwise. Always provide the full file content — partial writes are not supported.",
        Parameters:
        [
            new ToolParameter("path",    "string", "Relative file path inside the repository, e.g. \"src/Foo.cs\".", Required: true),
            new ToolParameter("content", "string", "Complete new file content.",                                       Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string path, content;
        try
        {
            (path, content) = ParseArgs(call.ArgumentsJson);
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

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(Success: false, Content: $"Write error: {ex.Message}"));
        }

        var lines = content.Split('\n').Length;
        return Task.FromResult(new ToolResult(Success: true, Content: $"Written: {path} ({lines} lines)"));
    }

    private static (string path, string content) ParseArgs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(pathEl.GetString()))
            throw new ArgumentException("missing_required_field: 'path' is required and must be a non-empty string.");

        if (!root.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("missing_required_field: 'content' is required and must be a string.");

        return (pathEl.GetString()!, contentEl.GetString()!);
    }
}
