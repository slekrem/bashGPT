using System.Text.Json;
using System.Text.RegularExpressions;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Filesystem;

public sealed class FilesystemSearchTool : ITool
{
    private const int MaxMatches = 200;
    private const int SnippetContextLines = 1;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
            (pattern, searchPath, glob, ignoreCase, maxMatches) = ParseInput(call.ArgumentsJson, call.WorkingDirectory);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
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
            return new ToolResult(Success: true, Content: JsonSerializer.Serialize(result, JsonOptions));
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

    private static (string Pattern, string Path, string Glob, bool IgnoreCase, int MaxMatches) ParseInput(string json, string? workingDirectory = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("pattern", out var patternEl))
            throw new ArgumentException("missing_required_field: 'pattern' is required. Example: {\"pattern\":\"TODO\",\"path\":\".\",\"glob\":\"*.cs\"}");
        if (patternEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'pattern' must be a string.");
        var pattern = patternEl.GetString();
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("invalid_value: 'pattern' must not be empty.");

        var path = ReadOptionalString(root, "path") ?? workingDirectory ?? Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(path))
            path = workingDirectory ?? Directory.GetCurrentDirectory();

        var glob = ReadOptionalString(root, "glob") ?? string.Empty;
        bool ignoreCase = ReadOptionalBool(root, "ignoreCase") ?? false;
        int maxMatches = ReadOptionalInt(root, "maxMatches") ?? MaxMatches;
        if (maxMatches <= 0)
            throw new ArgumentException("invalid_value: 'maxMatches' must be greater than 0.");

        return (pattern, path, glob, ignoreCase, maxMatches);
    }

    private static string? ReadOptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.String => valueEl.GetString(),
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be a string."),
        };
    }

    private static bool? ReadOptionalBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be a boolean."),
        };
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
