using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Filesystem;

namespace bashGPT.Tools.Filesystem.Tests;

public class FilesystemSearchToolTests : IDisposable
{
    private readonly string _tempDir;

    public FilesystemSearchToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FilesystemSearchTool CreateTool() =>
        new(new DefaultFilesystemPolicy([_tempDir]));

    private static ToolCall Call(string pattern, string? path = null, string? glob = null, bool? ignoreCase = null, int? maxMatches = null)
    {
        var args = new Dictionary<string, object?> { ["pattern"] = pattern };
        if (path is not null) args["path"] = path;
        if (glob is not null) args["glob"] = glob;
        if (ignoreCase is not null) args["ignoreCase"] = ignoreCase;
        if (maxMatches is not null) args["maxMatches"] = maxMatches;
        return new ToolCall("filesystem_search", JsonSerializer.Serialize(args));
    }

    [Fact]
    public async Task ExecuteAsync_PatternFound_ReturnsMatches()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "a.txt"), "hello world\nfoo bar");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "b.txt"), "no match here");

        var result = await CreateTool().ExecuteAsync(Call("hello", _tempDir), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("totalMatches").GetInt32());
        var match = output.GetProperty("matches")[0];
        Assert.Equal(1, match.GetProperty("line").GetInt32());
        Assert.Contains("hello", match.GetProperty("snippet").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_NoMatches_ReturnsEmptyList()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "empty.txt"), "nothing relevant");

        var result = await CreateTool().ExecuteAsync(Call("xyz123", _tempDir), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(0, output.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_GlobFilter_OnlyMatchesGlob()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "match.cs"), "target text");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "skip.txt"), "target text");

        var result = await CreateTool().ExecuteAsync(Call("target", _tempDir, glob: "*.cs"), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("totalMatches").GetInt32());
        Assert.Contains(".cs", output.GetProperty("matches")[0].GetProperty("file").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_IgnoreCase_FindsCaseInsensitive()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "case.txt"), "Hello World");

        var result = await CreateTool().ExecuteAsync(Call("hello", _tempDir, ignoreCase: true), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_MaxMatches_Truncates()
    {
        for (var i = 0; i < 10; i++)
            await File.WriteAllTextAsync(Path.Combine(_tempDir, $"f{i}.txt"), "match this");

        var result = await CreateTool().ExecuteAsync(Call("match", _tempDir, maxMatches: 3), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(3, output.GetProperty("totalMatches").GetInt32());
        Assert.True(output.GetProperty("truncated").GetBoolean());
    }

    [Fact]
    public async Task ExecuteAsync_PathOutsideRoot_ReturnsFailure()
    {
        var result = await CreateTool().ExecuteAsync(Call("anything", "/etc"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRegex_ReturnsFailure()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "x.txt"), "content");

        var result = await CreateTool().ExecuteAsync(Call("[invalid(regex", _tempDir), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid regex", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPattern_ReturnsStructuredValidationError()
    {
        var call = new ToolCall("filesystem_search", """{"path":"."}""");

        var result = await CreateTool().ExecuteAsync(call, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'pattern'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyPath_UsesCurrentDirectory()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "needle.txt"), "abc needle xyz");
        var cwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir);
            // Tool nach SetCurrentDirectory erstellen, damit die Policy den vom OS aufgelösten
            // Pfad erhält (auf macOS: /tmp → /private/tmp).
            var resolvedDir = Directory.GetCurrentDirectory();
            var tool = new FilesystemSearchTool(new DefaultFilesystemPolicy([resolvedDir]));
            var call = new ToolCall("filesystem_search", """{"pattern":"needle","path":""}""");

            var result = await tool.ExecuteAsync(call, CancellationToken.None);

            Assert.True(result.Success);
            var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
            Assert.Equal(1, output.GetProperty("totalMatches").GetInt32());
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
        }
    }
}
