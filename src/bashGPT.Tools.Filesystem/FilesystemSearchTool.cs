using System.Text.Json;
using System.Text.RegularExpressions;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Filesystem;

public sealed class FilesystemSearchTool : ITool
{
    private const int MaxMatches = 200;
    private const int SnippetContextLines = 1;

    private readonly IFilesystemPolicy _policy;

    public FilesystemSearchTool(IFilesystemPolicy? policy = null)
    {
        _policy = policy ?? new DefaultFilesystemPolicy();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "filesystem_search",
        Description: "Searches for a text pattern (regex) in files within a directory. Returns file, line number and snippet for each match.",
        Parameters:
        [
            new ToolParameter("pattern", "string", "Regular expression pattern to search for.", Required: true),
            new ToolParameter("path", "string", "Directory to search in. Default: current working directory.", Required: false),
            new ToolParameter("glob", "string", "File glob pattern to filter files, e.g. '*.cs'. Default: all files.", Required: false),
            new ToolParameter("ignoreCase", "boolean", "Case-insensitive search. Default: false.", Required: false),
            new ToolParameter("maxMatches", "integer", $"Maximum number of matches to return. Default: {MaxMatches}.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string pattern, searchPath, glob;
        bool ignoreCase;
        int maxMatches;
        try
        {
            (pattern, searchPath, glob, ignoreCase, maxMatches) = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        var absolutePath = Path.GetFullPath(searchPath);

        if (!_policy.AllowRead(absolutePath))
            return new ToolResult(Success: false, Content: $"Read blocked by policy: {absolutePath}");

        if (!Directory.Exists(absolutePath))
            return new ToolResult(Success: false, Content: $"Directory not found: {absolutePath}");

        try
        {
            var matches = await SearchAsync(pattern, absolutePath, glob, ignoreCase, maxMatches, ct);
            var result = new
            {
                pattern,
                path = absolutePath,
                totalMatches = matches.Count,
                truncated = matches.Count >= maxMatches,
                matches,
            };
            return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        catch (RegexParseException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid regex pattern: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Search failed: {ex.Message}");
        }
    }

    private static async Task<List<object>> SearchAsync(
        string pattern, string rootPath, string glob, bool ignoreCase, int maxMatches, CancellationToken ct)
    {
        var regexOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var regex = new Regex(pattern, regexOptions | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        var searchOption = SearchOption.AllDirectories;
        var files = string.IsNullOrWhiteSpace(glob)
            ? Directory.EnumerateFiles(rootPath, "*", searchOption)
            : Directory.EnumerateFiles(rootPath, glob, searchOption);

        var results = new List<object>();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            if (results.Count >= maxMatches) break;

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(file, ct);
            }
            catch
            {
                continue; // skip unreadable files (binary, locked, etc.)
            }

            for (var i = 0; i < lines.Length && results.Count < maxMatches; i++)
            {
                if (!regex.IsMatch(lines[i])) continue;

                var contextStart = Math.Max(0, i - SnippetContextLines);
                var contextEnd = Math.Min(lines.Length - 1, i + SnippetContextLines);
                var snippet = string.Join('\n', lines[contextStart..(contextEnd + 1)]);

                results.Add(new
                {
                    file,
                    line = i + 1,
                    snippet,
                });
            }
        }

        return results;
    }

    private static (string Pattern, string Path, string Glob, bool IgnoreCase, int MaxMatches) ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var pattern = root.GetProperty("pattern").GetString()
            ?? throw new ArgumentException("pattern must not be null");
        var path = root.TryGetProperty("path", out var pathEl) ? pathEl.GetString() ?? Directory.GetCurrentDirectory() : Directory.GetCurrentDirectory();
        var glob = root.TryGetProperty("glob", out var globEl) ? globEl.GetString() ?? string.Empty : string.Empty;
        bool ignoreCase = root.TryGetProperty("ignoreCase", out var icEl) && icEl.GetBoolean();
        int maxMatches = root.TryGetProperty("maxMatches", out var mmEl) ? mmEl.GetInt32() : MaxMatches;

        return (pattern, path, glob, ignoreCase, maxMatches);
    }
}
