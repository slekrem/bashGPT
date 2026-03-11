using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Filesystem;

namespace bashGPT.Tools.Filesystem.Tests;

public class FilesystemReadToolTests : IDisposable
{
    private readonly string _tempDir;

    public FilesystemReadToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FilesystemReadTool CreateTool() =>
        new(new DefaultFilesystemPolicy([_tempDir]));

    private static ToolCall Call(string path, int? startLine = null, int? endLine = null)
    {
        var args = new Dictionary<string, object?> { ["path"] = path };
        if (startLine is not null) args["startLine"] = startLine;
        if (endLine is not null) args["endLine"] = endLine;
        return new ToolCall("filesystem_read", JsonSerializer.Serialize(args));
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFile_ReturnsContent()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        await File.WriteAllTextAsync(file, "line1\nline2\nline3");

        var result = await CreateTool().ExecuteAsync(Call(file), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Contains("line1", output.GetProperty("content").GetString());
        Assert.Equal(3, output.GetProperty("totalLines").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_LineRange_ReturnsSlice()
    {
        var file = Path.Combine(_tempDir, "range.txt");
        await File.WriteAllTextAsync(file, "a\nb\nc\nd\ne");

        var result = await CreateTool().ExecuteAsync(Call(file, startLine: 2, endLine: 4), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        var content = output.GetProperty("content").GetString()!;
        Assert.Contains("b", content);
        Assert.Contains("d", content);
        Assert.DoesNotContain("a", content);
        Assert.DoesNotContain("e", content);
    }

    [Fact]
    public async Task ExecuteAsync_FileNotFound_ReturnsFailure()
    {
        var result = await CreateTool().ExecuteAsync(Call(Path.Combine(_tempDir, "missing.txt")), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_PathOutsideRoot_ReturnsFailure()
    {
        var result = await CreateTool().ExecuteAsync(Call("/etc/passwd"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_PathTraversal_ReturnsFailure()
    {
        var file = Path.Combine(_tempDir, "..", "escape.txt");
        var result = await CreateTool().ExecuteAsync(Call(file), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        var result = await CreateTool().ExecuteAsync(new ToolCall("filesystem_read", "{bad}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPath_ReturnsStructuredValidationError()
    {
        var result = await CreateTool().ExecuteAsync(new ToolCall("filesystem_read", """{"startLine":1}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'path'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EndBeforeStart_ReturnsStructuredValidationError()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        await File.WriteAllTextAsync(file, "line1\nline2\nline3");

        var result = await CreateTool().ExecuteAsync(Call(file, startLine: 3, endLine: 2), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'endLine'", result.Content, StringComparison.Ordinal);
    }
}
