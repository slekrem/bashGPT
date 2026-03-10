using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Filesystem;

public sealed class FilesystemWriteTool : ITool
{
    private readonly IFilesystemPolicy _policy;

    public FilesystemWriteTool(IFilesystemPolicy? policy = null)
    {
        _policy = policy ?? new DefaultFilesystemPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "filesystem_write",
        Description: "Writes content to a file. By default does not overwrite existing files unless overwrite=true.",
        Parameters:
        [
            new ToolParameter("path", "string", "Absolute or relative path to the file.", Required: true),
            new ToolParameter("content", "string", "Content to write.", Required: true),
            new ToolParameter("overwrite", "boolean", "Allow overwriting an existing file. Default: false.", Required: false),
            new ToolParameter("createDirectories", "boolean", "Create parent directories if they don't exist. Default: true.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string path, content;
        bool overwrite, createDirectories;
        try
        {
            (path, content, overwrite, createDirectories) = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        var absolute = Path.GetFullPath(path);

        if (!_policy.AllowWrite(absolute))
            return new ToolResult(Success: false, Content: $"Write blocked by policy: {absolute}");

        if (File.Exists(absolute) && !overwrite)
            return new ToolResult(Success: false, Content: $"File already exists (use overwrite=true to replace): {absolute}");

        try
        {
            var dir = Path.GetDirectoryName(absolute);
            if (dir is not null && !Directory.Exists(dir))
            {
                if (createDirectories)
                    Directory.CreateDirectory(dir);
                else
                    return new ToolResult(Success: false, Content: $"Directory does not exist: {dir}");
            }

            await File.WriteAllTextAsync(absolute, content, ct);

            var result = new { path = absolute, bytesWritten = System.Text.Encoding.UTF8.GetByteCount(content) };
            return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Write failed: {ex.Message}");
        }
    }

    private static (string Path, string Content, bool Overwrite, bool CreateDirectories) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var path = root.GetProperty("path").GetString()
            ?? throw new ArgumentException("path must not be null");
        var content = root.GetProperty("content").GetString()
            ?? throw new ArgumentException("content must not be null");
        bool overwrite = root.TryGetProperty("overwrite", out var ow) && ow.GetBoolean();
        bool createDirectories = !root.TryGetProperty("createDirectories", out var cd) || cd.GetBoolean();

        return (path, content, overwrite, createDirectories);
    }
}
