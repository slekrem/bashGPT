using System.Text.Json;
using System.Text.Json.Serialization;

namespace bashGPT.Core.Configuration;

public class ConfigurationService
{
    protected virtual string ConfigFile => AppBootstrap.GetDefaultConfigFilePath();

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
                    $"Configuration file '{ConfigFile}' is invalid: {ex.Message}",
                    ex);
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
                if (!bool.TryParse(value, out var forceTools))
                {
                    throw new ArgumentException(
                        $"Invalid value for 'forceTools': '{value}'. Allowed: true, false");
                }

                config.DefaultForceTools = forceTools;
                break;

            default:
                throw new ArgumentException(
                    $"Unknown configuration key '{key}'.\n" +
                    "Valid keys: forceTools, ollama.baseUrl, ollama.model");
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
            _ => throw new ArgumentException($"Unknown configuration key '{key}'.")
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

        if (Environment.GetEnvironmentVariable("BASHGPT_FORCE_TOOLS") is { } forceTools
            && bool.TryParse(forceTools, out var forceToolsEnabled))
        {
            config.DefaultForceTools = forceToolsEnabled;
        }
    }
}
