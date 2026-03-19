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
                    $"Konfigurationsdatei '{ConfigFile}' ist ungültig: {ex.Message}", ex);
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
                if (!string.Equals(value, "ollama", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Ungültiger Provider '{value}'. Erlaubt: ollama");
                config.DefaultProvider = ProviderType.Ollama;
                break;

            case "ollama.baseurl":
                config.Ollama.BaseUrl = value;
                break;

            case "ollama.model":
                config.Ollama.Model = value;
                break;

            case "forcetools":
            case "defaultforcetools":
                if (!bool.TryParse(value, out var ft))
                    throw new ArgumentException($"Ungültiger Wert für 'forceTools': '{value}'. Erlaubt: true, false");
                config.DefaultForceTools = ft;
                break;

            default:
                throw new ArgumentException(
                    $"Unbekannter Konfigurationsschlüssel '{key}'.\n" +
                    "Gültige Schlüssel: defaultProvider, forceTools, " +
                    "ollama.baseUrl, ollama.model");
        }

        await SaveAsync(config);
    }

    public async Task<string> GetAsync(string key)
    {
        var config = await LoadAsync();

        return key.ToLowerInvariant() switch
        {
            "defaultprovider" or "provider" => config.DefaultProvider.ToString().ToLowerInvariant(),
            "forcetools" or "defaultforcetools" => config.DefaultForceTools.ToString().ToLowerInvariant(),
            "ollama.baseurl" => config.Ollama.BaseUrl,
            "ollama.model" => config.Ollama.Model,
            _ => throw new ArgumentException($"Unbekannter Konfigurationsschlüssel '{key}'.")
        };
    }

    public async Task<string> ListAsync()
    {
        var config = await LoadAsync();
        return $"""
            defaultProvider  = {config.DefaultProvider.ToString().ToLowerInvariant()}
            forceTools       = {config.DefaultForceTools.ToString().ToLowerInvariant()}
            ollama.baseUrl   = {config.Ollama.BaseUrl}
            ollama.model     = {config.Ollama.Model}
            """;
    }

    private static void ApplyEnvironmentOverrides(AppConfig config)
    {
        var provider = Environment.GetEnvironmentVariable("BASHGPT_PROVIDER");
        if (provider is not null
            && Enum.TryParse<ProviderType>(provider, ignoreCase: true, out var p)
            && p == ProviderType.Ollama)
            config.DefaultProvider = p;

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
