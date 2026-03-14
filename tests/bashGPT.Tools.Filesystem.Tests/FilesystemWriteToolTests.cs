using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Filesystem;

namespace bashGPT.Tools.Filesystem.Tests;

public class FilesystemWriteToolTests : IDisposable
{
    private readonly string _tempDir;

    public FilesystemWriteToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FilesystemWriteTool CreateTool() =>
        new(new DefaultFilesystemPolicy([_tempDir]));

    private static ToolCall Call(string path, string content, bool? overwrite = null, bool? createDirectories = null)
    {
        var args = new Dictionary<string, object?> { ["path"] = path, ["content"] = content };
        if (overwrite is not null) args["overwrite"] = overwrite;
        if (createDirectories is not null) args["createDirectories"] = createDirectories;
        return new ToolCall("filesystem_write", JsonSerializer.Serialize(args));
    }

    [Fact]
    public async Task ExecuteAsync_NewFile_WritesSuccessfully()
    {
        var file = Path.Combine(_tempDir, "new.txt");

        var result = await CreateTool().ExecuteAsync(Call(file, "hello world"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("hello world", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFileWithoutOverwrite_ReturnsFailure()
    {
        var file = Path.Combine(_tempDir, "existing.txt");
        await File.WriteAllTextAsync(file, "original");

        var result = await CreateTool().ExecuteAsync(Call(file, "new content"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("overwrite=true", result.Content);
        Assert.Equal("original", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFileWithOverwrite_Overwrites()
    {
        var file = Path.Combine(_tempDir, "overwrite.txt");
        await File.WriteAllTextAsync(file, "old");

        var result = await CreateTool().ExecuteAsync(Call(file, "new", overwrite: true), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("new", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesParentDirectories()
    {
        var file = Path.Combine(_tempDir, "sub", "deep", "file.txt");

        var result = await CreateTool().ExecuteAsync(Call(file, "content"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task ExecuteAsync_PathOutsideRoot_ReturnsFailure()
    {
        var result = await CreateTool().ExecuteAsync(Call("/tmp/evil.txt", "data"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCorrectByteCount()
    {
        var file = Path.Combine(_tempDir, "bytes.txt");
        var content = "hello";

        var result = await CreateTool().ExecuteAsync(Call(file, content), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(5, output.GetProperty("bytesWritten").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_MissingPath_ReturnsStructuredValidationError()
    {
        var result = await CreateTool().ExecuteAsync(new ToolCall("filesystem_write", """{"content":"x"}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'path'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidOverwriteType_ReturnsStructuredValidationError()
    {
        var file = Path.Combine(_tempDir, "new.txt");
        var result = await CreateTool().ExecuteAsync(
            new ToolCall("filesystem_write", $$"""{"path":{{JsonSerializer.Serialize(file)}},"content":"x","overwrite":"yes"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'overwrite'", result.Content, StringComparison.Ordinal);
    }
}
