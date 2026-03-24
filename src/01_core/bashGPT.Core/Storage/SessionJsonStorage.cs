using System.Text.Json;
using System.Text.Json.Serialization;

namespace bashGPT.Core.Storage;

internal static class SessionJsonStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<T?> ReadAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) // codeql[cs/path-injection]
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path); // codeql[cs/path-injection]
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static async Task WriteAsync<T>(string path, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await WriteRawAsync(path, json);
    }

    public static async Task WriteRawAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, content); // codeql[cs/path-injection]
        File.Move(tempPath, path, overwrite: true);
    }
}
