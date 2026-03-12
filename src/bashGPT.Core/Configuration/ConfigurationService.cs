using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Shell;

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

        ApplyDefaultNormalization(config);
        ApplyEnvironmentOverrides(config);
        ApplyDefaultNormalization(config);
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
            case "ollama.temperature":
                config.Ollama.Temperature = ParseDouble(value, "ollama.temperature");
                break;
            case "ollama.topp":
            case "ollama.top_p":
                config.Ollama.TopP = ParseDouble(value, "ollama.topP");
                break;
            case "ollama.seed":
                config.Ollama.Seed = ParseInt(value, "ollama.seed");
                break;
            case "ollama.numctx":
            case "ollama.num_ctx":
                config.Ollama.NumCtx = ParsePositiveInt(value, "ollama.numCtx");
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
            case "cerebras.temperature":
                config.Cerebras.Temperature = ParseDouble(value, "cerebras.temperature");
                break;
            case "cerebras.topp":
            case "cerebras.top_p":
                config.Cerebras.TopP = ParseDouble(value, "cerebras.topP");
                break;
            case "cerebras.maxcompletiontokens":
            case "cerebras.max_completion_tokens":
                config.Cerebras.MaxCompletionTokens = ParseInt(value, "cerebras.maxCompletionTokens");
                break;
            case "cerebras.seed":
                config.Cerebras.Seed = ParseInt(value, "cerebras.seed");
                break;
            case "cerebras.reasoningeffort":
            case "cerebras.reasoning_effort":
                config.Cerebras.ReasoningEffort = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                break;
            case "commandtimeoutseconds":
            {
                var timeout = ParseInt(value, "commandTimeoutSeconds");
                if (timeout <= 0)
                    throw new ArgumentException($"Ungültiger Wert für 'commandTimeoutSeconds': '{value}'. Muss größer als 0 sein.");
                config.CommandTimeoutSeconds = timeout;
                break;
            }
            case "execmode":
            case "defaultexecmode":
                config.DefaultExecMode = ExecModeConverter.Parse(value)
                    ?? throw new ArgumentException($"Ungültiger ExecMode '{value}'. Erlaubt: ask, auto-exec, dry-run, no-exec");
                break;
            case "forcetools":
            case "defaultforcetools":
                if (!bool.TryParse(value, out var ft))
                    throw new ArgumentException($"Ungültiger Wert für 'forceTools': '{value}'. Erlaubt: true, false");
                config.DefaultForceTools = ft;
                break;
            case "loopdetectionenabled":
                if (!bool.TryParse(value, out var ld))
                    throw new ArgumentException($"Ungültiger Wert für 'loopDetectionEnabled': '{value}'. Erlaubt: true, false");
                config.LoopDetectionEnabled = ld;
                break;
            case "maxtoolcallrounds":
                config.MaxToolCallRounds = ParseInt(value, "maxToolCallRounds");
                break;
            default:
                throw new ArgumentException(
                    $"Unbekannter Konfigurationsschlüssel '{key}'.\n" +
                    "Gültige Schlüssel: defaultProvider, commandTimeoutSeconds, execMode, forceTools, " +
                    "loopDetectionEnabled, maxToolCallRounds, " +
                    "ollama.baseUrl, ollama.model, ollama.temperature, ollama.topP, ollama.seed, ollama.numCtx, " +
                    "cerebras.apiKey, cerebras.model, cerebras.baseUrl, cerebras.temperature, " +
                    "cerebras.topP, cerebras.maxCompletionTokens, cerebras.seed, cerebras.reasoningEffort");
        }

        await SaveAsync(config);
    }

    public async Task<string> GetAsync(string key)
    {
        var config = await LoadAsync();

        return key.ToLowerInvariant() switch
        {
            "defaultprovider" or "provider" => config.DefaultProvider.ToString().ToLower(),
            "commandtimeoutseconds" => config.CommandTimeoutSeconds.ToString(),
            "execmode" or "defaultexecmode" => ExecModeConverter.ToString(config.DefaultExecMode),
            "forcetools" or "defaultforcetools" => config.DefaultForceTools.ToString().ToLower(),
            "loopdetectionenabled" => config.LoopDetectionEnabled.ToString().ToLower(),
            "maxtoolcallrounds" => config.MaxToolCallRounds.ToString(),
            "ollama.baseurl" => config.Ollama.BaseUrl,
            "ollama.model" => config.Ollama.Model,
            "ollama.temperature" => config.Ollama.Temperature?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)",
            "ollama.topp" or "ollama.top_p" => config.Ollama.TopP?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)",
            "ollama.seed" => config.Ollama.Seed?.ToString() ?? "(nicht gesetzt)",
            "ollama.numctx" or "ollama.num_ctx" => config.Ollama.NumCtx?.ToString() ?? "(nicht gesetzt)",
            "cerebras.apikey" => config.Cerebras.ApiKey is not null ? "***" : "(nicht gesetzt)",
            "cerebras.model" => config.Cerebras.Model,
            "cerebras.baseurl" => config.Cerebras.BaseUrl,
            "cerebras.temperature" => config.Cerebras.Temperature?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)",
            "cerebras.topp" or "cerebras.top_p" => config.Cerebras.TopP?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)",
            "cerebras.maxcompletiontokens" or "cerebras.max_completion_tokens" => config.Cerebras.MaxCompletionTokens?.ToString() ?? "(nicht gesetzt)",
            "cerebras.seed" => config.Cerebras.Seed?.ToString() ?? "(nicht gesetzt)",
            "cerebras.reasoningeffort" or "cerebras.reasoning_effort" => config.Cerebras.ReasoningEffort ?? "(nicht gesetzt)",
            _ => throw new ArgumentException($"Unbekannter Konfigurationsschlüssel '{key}'.")
        };
    }

    public async Task<string> ListAsync()
    {
        var config = await LoadAsync();
        return $"""
            defaultProvider  = {config.DefaultProvider.ToString().ToLower()}
            commandTimeoutSeconds = {config.CommandTimeoutSeconds}
            execMode         = {ExecModeConverter.ToString(config.DefaultExecMode)}
            forceTools       = {config.DefaultForceTools.ToString().ToLower()}
            loopDetectionEnabled = {config.LoopDetectionEnabled.ToString().ToLower()}
            maxToolCallRounds = {config.MaxToolCallRounds}
            ollama.baseUrl   = {config.Ollama.BaseUrl}
            ollama.model     = {config.Ollama.Model}
            ollama.temperature = {config.Ollama.Temperature?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)"}
            ollama.topP        = {config.Ollama.TopP?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)"}
            ollama.seed        = {config.Ollama.Seed?.ToString() ?? "(nicht gesetzt)"}
            ollama.numCtx      = {config.Ollama.NumCtx?.ToString() ?? "(nicht gesetzt)"}
            cerebras.apiKey  = {(config.Cerebras.ApiKey is not null ? "***" : "(nicht gesetzt)")}
            cerebras.model   = {config.Cerebras.Model}
            cerebras.baseUrl = {config.Cerebras.BaseUrl}
            cerebras.temperature = {config.Cerebras.Temperature?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)"}
            cerebras.topP        = {config.Cerebras.TopP?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(nicht gesetzt)"}
            cerebras.maxCompletionTokens = {config.Cerebras.MaxCompletionTokens?.ToString() ?? "(nicht gesetzt)"}
            cerebras.seed        = {config.Cerebras.Seed?.ToString() ?? "(nicht gesetzt)"}
            cerebras.reasoningEffort = {config.Cerebras.ReasoningEffort ?? "(nicht gesetzt)"}
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

        var cerebrasTemperature = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_TEMPERATURE");
        if (!string.IsNullOrWhiteSpace(cerebrasTemperature))
            config.Cerebras.Temperature = ParseDouble(cerebrasTemperature, "BASHGPT_CEREBRAS_TEMPERATURE");

        var cerebrasTopP = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_TOP_P");
        if (!string.IsNullOrWhiteSpace(cerebrasTopP))
            config.Cerebras.TopP = ParseDouble(cerebrasTopP, "BASHGPT_CEREBRAS_TOP_P");

        var cerebrasMaxCompletionTokens = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_MAX_COMPLETION_TOKENS");
        if (!string.IsNullOrWhiteSpace(cerebrasMaxCompletionTokens))
            config.Cerebras.MaxCompletionTokens = ParseInt(cerebrasMaxCompletionTokens, "BASHGPT_CEREBRAS_MAX_COMPLETION_TOKENS");

        var cerebrasSeed = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_SEED");
        if (!string.IsNullOrWhiteSpace(cerebrasSeed))
            config.Cerebras.Seed = ParseInt(cerebrasSeed, "BASHGPT_CEREBRAS_SEED");

        var cerebrasReasoningEffort = Environment.GetEnvironmentVariable("BASHGPT_CEREBRAS_REASONING_EFFORT");
        if (!string.IsNullOrWhiteSpace(cerebrasReasoningEffort))
            config.Cerebras.ReasoningEffort = cerebrasReasoningEffort.Trim();

        var ollamaUrl = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_URL");
        if (ollamaUrl is not null)
            config.Ollama.BaseUrl = ollamaUrl;

        var ollamaModel = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_MODEL");
        if (ollamaModel is not null)
            config.Ollama.Model = ollamaModel;

        var ollamaTemperature = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_TEMPERATURE");
        if (!string.IsNullOrWhiteSpace(ollamaTemperature))
            config.Ollama.Temperature = ParseDouble(ollamaTemperature, "BASHGPT_OLLAMA_TEMPERATURE");

        var ollamaTopP = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_TOP_P");
        if (!string.IsNullOrWhiteSpace(ollamaTopP))
            config.Ollama.TopP = ParseDouble(ollamaTopP, "BASHGPT_OLLAMA_TOP_P");

        var ollamaSeed = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_SEED");
        if (!string.IsNullOrWhiteSpace(ollamaSeed))
            config.Ollama.Seed = ParseInt(ollamaSeed, "BASHGPT_OLLAMA_SEED");

        var ollamaNumCtx = Environment.GetEnvironmentVariable("BASHGPT_OLLAMA_NUM_CTX");
        if (!string.IsNullOrWhiteSpace(ollamaNumCtx))
            config.Ollama.NumCtx = ParsePositiveInt(ollamaNumCtx, "BASHGPT_OLLAMA_NUM_CTX");

        if (int.TryParse(Environment.GetEnvironmentVariable("BASHGPT_COMMAND_TIMEOUT"), out var t) && t > 0)
            config.CommandTimeoutSeconds = t;

        if (ExecModeConverter.Parse(Environment.GetEnvironmentVariable("BASHGPT_EXEC_MODE")) is { } em)
            config.DefaultExecMode = em;

        if (Environment.GetEnvironmentVariable("BASHGPT_FORCE_TOOLS") is { } fts
            && bool.TryParse(fts, out var ftBool))
            config.DefaultForceTools = ftBool;

        if (Environment.GetEnvironmentVariable("BASHGPT_LOOP_DETECTION") is { } ld
            && bool.TryParse(ld, out var ldBool))
            config.LoopDetectionEnabled = ldBool;

        if (Environment.GetEnvironmentVariable("BASHGPT_MAX_TOOL_CALL_ROUNDS") is { } mr
            && int.TryParse(mr, out var mrInt) && mrInt > 0)
            config.MaxToolCallRounds = mrInt;
    }

    private static void ApplyDefaultNormalization(AppConfig config)
    {
        config.Ollama.Temperature ??= 0.2;
        config.Ollama.TopP ??= 0.9;
        config.Ollama.NumCtx ??= 65536;

        config.Cerebras.Temperature ??= 0.2;
        config.Cerebras.TopP ??= 0.9;
        config.Cerebras.MaxCompletionTokens ??= 2048;
        config.Cerebras.ReasoningEffort ??= "medium";
    }

    private static double ParseDouble(string value, string key)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            throw new ArgumentException($"Ungültiger Wert für '{key}': '{value}'");
        return parsed;
    }

    private static int ParseInt(string value, string key)
    {
        if (!int.TryParse(value, out var parsed))
            throw new ArgumentException($"Ungültiger Wert für '{key}': '{value}'");
        return parsed;
    }

    private static int ParsePositiveInt(string value, string key)
    {
        var parsed = ParseInt(value, key);
        if (parsed <= 0)
            throw new ArgumentException($"Ungültiger Wert für '{key}': '{value}'. Muss größer als 0 sein.");
        return parsed;
    }
}
