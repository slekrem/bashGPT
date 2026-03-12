using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Filesystem;

public sealed class FilesystemReadTool : ITool
{
    private const int MaxChars = 131_072; // 128 KB

    private readonly IFilesystemPolicy _policy;

    public FilesystemReadTool(IFilesystemPolicy? policy = null)
    {
        _policy = policy ?? new DefaultFilesystemPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "filesystem_read",
        Description: "Reads a file and returns its content. Optionally limited to a line range.",
        Parameters:
        [
            new ToolParameter("path", "string", "Absolute or relative path to the file.", Required: true),
            new ToolParameter("startLine", "integer", "First line to read (1-based, inclusive). Default: 1.", Required: false),
            new ToolParameter("endLine", "integer", "Last line to read (1-based, inclusive). Default: all lines.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string path;
        int startLine, endLine;
        try
        {
            (path, startLine, endLine) = ParseInput(call.ArgumentsJson);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }

        var absolute = Path.GetFullPath(path);

        if (!_policy.AllowRead(absolute))
            return new ToolResult(Success: false, Content: $"Read blocked by policy: {absolute}");

        if (!File.Exists(absolute))
            return new ToolResult(Success: false, Content: $"File not found: {absolute}");

        try
        {
            var lines = await File.ReadAllLinesAsync(absolute, ct);
            var totalLines = lines.Length;

            var from = Math.Max(1, startLine) - 1;
            var to = endLine > 0 ? Math.Min(endLine, totalLines) : totalLines;
            var slice = lines[from..to];

            var content = string.Join('\n', slice);
            if (content.Length > MaxChars)
                content = content[..MaxChars] + "\n[truncated]";

            var result = new
            {
                path = absolute,
                totalLines,
                startLine = from + 1,
                endLine = to,
                content,
            };
            return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Read failed: {ex.Message}");
        }
    }

    private static (string Path, int StartLine, int EndLine) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("path", out var pathEl))
            throw new ArgumentException("missing_required_field: 'path' is required. Example: {\"path\":\"src/Program.cs\",\"startLine\":1,\"endLine\":200}");
        if (pathEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'path' must be a string.");
        var path = pathEl.GetString();
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("invalid_value: 'path' must not be empty.");

        int startLine = ReadOptionalInt(root, "startLine") ?? 1;
        int endLine = ReadOptionalInt(root, "endLine") ?? 0;
        if (startLine < 1)
            throw new ArgumentException("invalid_value: 'startLine' must be >= 1.");
        if (endLine < 0)
            throw new ArgumentException("invalid_value: 'endLine' must be >= 0.");
        if (endLine > 0 && endLine < startLine)
            throw new ArgumentException("invalid_value: 'endLine' must be >= 'startLine' when set.");

        return (path, startLine, endLine);
    }

    private static int? ReadOptionalInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.Number when valueEl.TryGetInt32(out var i) => i,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be an integer."),
        };
    }
}
