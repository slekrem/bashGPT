using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Configuration;

public class ConfigurationService
{
    private static readonly string DefaultConfigFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
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
                if (!Enum.TryParse<ProviderType>(value, ignoreCase: true, out var provider))
                    throw new ArgumentException($"Ungültiger Provider '{value}'. Erlaubt: ollama, cerebras");
                config.DefaultProvider = provider;
                break;
            case "ollama.baseurl":
                config.Ollama.BaseUrl = value;
                break;
            case "ollama.model":
                config.Ollama.Model = value;
                break;
            case "cerebras.apikey":
                config.Cerebras.ApiKey = value;
                break;
            case "cerebras.model":
                config.Cerebras.Model = value;
                break;
            case "cerebras.baseurl":
                config.Cerebras.BaseUrl = value;
                break;
            default:
                throw new ArgumentException(
                    $"Unbekannter Konfigurationsschlüssel '{key}'.\n" +
                    "Gültige Schlüssel: defaultProvider, ollama.baseUrl, ollama.model, " +
                    "cerebras.apiKey, cerebras.model, cerebras.baseUrl");
        }

        await SaveAsync(config);
    }

    public async Task<string> GetAsync(string key)
    {
        var config = await LoadAsync();

        return key.ToLowerInvariant() switch
        {
            "defaultprovider" or "provider" => config.DefaultProvider.ToString().ToLower(),
            "ollama.baseurl" => config.Ollama.BaseUrl,
            "ollama.model" => config.Ollama.Model,
            "cerebras.apikey" => config.Cerebras.ApiKey is not null ? "***" : "(nicht gesetzt)",
            "cerebras.model" => config.Cerebras.Model,
            "cerebras.baseurl" => config.Cerebras.BaseUrl,
            _ => throw new ArgumentException($"Unbekannter Konfigurationsschlüssel '{key}'.")
        };
    }

    public async Task<string> ListAsync()
    {
        var config = await LoadAsync();
        return $"""
            defaultProvider  = {config.DefaultProvider.ToString().ToLower()}
            ollama.baseUrl   = {config.Ollama.BaseUrl}
            ollama.model     = {config.Ollama.Model}
            cerebras.apiKey  = {(config.Cerebras.ApiKey is not null ? "***" : "(nicht gesetzt)")}
            cerebras.model   = {config.Cerebras.Model}
            cerebras.baseUrl = {config.Cerebras.BaseUrl}
            """;
    }

    private static void ApplyEnvironmentOverrides(AppConfig config)
    {
        var provider = Environment.GetEnvironmentVariable("BASHGPT_PROVIDER");
        if (provider is not null && Enum.TryParse<ProviderType>(provider, ignoreCase: true, out var p))
            config.DefaultProvider = p;

        var cerebrasKey = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_KEY");
        if (cerebrasKey is not null)
            config.Cerebras.ApiKey = cerebrasKey;

        var cerebrasModel = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_MODEL");
        if (cerebrasModel is not null)
            config.Cerebras.Model = cerebrasModel;

        var ollamaUrl = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_URL");
        if (ollamaUrl is not null)
            config.Ollama.BaseUrl = ollamaUrl;

        var ollamaModel = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_MODEL");
        if (ollamaModel is not null)
            config.Ollama.Model = ollamaModel;
    }
}
