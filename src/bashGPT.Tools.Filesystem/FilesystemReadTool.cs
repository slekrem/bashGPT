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
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
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

        var path = root.GetProperty("path").GetString()
            ?? throw new ArgumentException("path must not be null");
        int startLine = root.TryGetProperty("startLine", out var sl) ? sl.GetInt32() : 1;
        int endLine = root.TryGetProperty("endLine", out var el) ? el.GetInt32() : 0;

        return (path, startLine, endLine);
    }
}
