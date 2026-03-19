using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Configuration;

public class ConfigurationService
{
    private static readonly string DefaultConfigFile =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "bashgpt", "config.json")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "bashgpt", "config.json");

    protected virtual string ConfigFile => DefaultConfigFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<AppConfig> LoadAsync()
    {
        AppConfig config = new();

        if (File.Exists(ConfigFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(ConfigFile);
                config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Konfigurationsdatei '{ConfigFile}' ist ungÃ¼ltig: {ex.Message}", ex);
            }
        }

        ApplyEnvironmentOverrides(config);
        return config;
    }

    public async Task SaveAsync(AppConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigFile)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(ConfigFile, json);
    }

    public async Task SetAsync(string key, string value)
    {
        var config = await LoadAsync();

        switch (key.ToLowerInvariant())
        {
            case "defaultprovider":
            case "provider":
                throw new ArgumentException("The 'provider' setting is obsolete. Ollama is the only supported provider.");

            case "ollama.baseurl":
                config.Ollama.BaseUrl = value;
                break;

            case "ollama.model":
                config.Ollama.Model = value;
                break;

            case "forcetools":
            case "defaultforcetools":
                if (!bool.TryParse(value, out var ft))
                    throw new ArgumentException($"UngÃ¼ltiger Wert fÃ¼r 'forceTools': '{value}'. Erlaubt: true, false");
                config.DefaultForceTools = ft;
                break;

            default:
                throw new ArgumentException(
                    $"Unbekannter KonfigurationsschlÃ¼ssel '{key}'.\n" +
                    "GÃ¼ltige SchlÃ¼ssel: forceTools, ollama.baseUrl, ollama.model");
        }

        await SaveAsync(config);
    }

    public async Task<string> GetAsync(string key)
    {
        var config = await LoadAsync();

        return key.ToLowerInvariant() switch
        {
            "defaultprovider" or "provider" => "ollama",
            "forcetools" or "defaultforcetools" => config.DefaultForceTools.ToString().ToLowerInvariant(),
            "ollama.baseurl" => config.Ollama.BaseUrl,
            "ollama.model" => config.Ollama.Model,
            _ => throw new ArgumentException($"Unbekannter KonfigurationsschlÃ¼ssel '{key}'.")
        };
    }

    public async Task<string> ListAsync()
    {
        var config = await LoadAsync();
        return $"""
            provider         = ollama
            forceTools       = {config.DefaultForceTools.ToString().ToLowerInvariant()}
            ollama.baseUrl   = {config.Ollama.BaseUrl}
            ollama.model     = {config.Ollama.Model}
            """;
    }

    private static void ApplyEnvironmentOverrides(AppConfig config)
    {
        var ollamaUrl = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_URL");
        if (ollamaUrl is not null)
            config.Ollama.BaseUrl = ollamaUrl;

        var ollamaModel = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_MODEL");
        if (ollamaModel is not null)
            config.Ollama.Model = ollamaModel;

        if (Environment.GetEnvironmentVariable("BASHGPT_FORCE_TOOLS") is { } fts
            && bool.TryParse(fts, out var ftBool))
            config.DefaultForceTools = ftBool;
    }
}
