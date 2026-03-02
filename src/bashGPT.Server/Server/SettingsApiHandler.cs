using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Shell;
using BashGPT.Providers;

namespace BashGPT.Server;

internal sealed class SettingsApiHandler(ConfigurationService? configService, ServerState state)
{
    private static readonly HttpClient MetadataHttp = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly TimeSpan ContextWindowCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly Dictionary<string, (DateTime fetchedAtUtc, int? value)> ContextWindowCache
        = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object ContextWindowCacheLock = new();

    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";

        if (ctx.Request.HttpMethod == "GET"  && path == "/api/settings")
        { await HandleGetAsync(ctx.Response, ct); return; }

        if (ctx.Request.HttpMethod == "PUT"  && path == "/api/settings")
        { await HandlePutAsync(ctx, ct); return; }

        if (ctx.Request.HttpMethod == "POST" && path == "/api/settings/test")
        { await HandleTestAsync(ctx.Response, ct); return; }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
    }

    // ── GET /api/settings ───────────────────────────────────────────────────

    private async Task HandleGetAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
            return;
        }
        var config = await configService.LoadAsync();
        var activeModel = config.DefaultProvider == ProviderType.Cerebras
            ? config.Cerebras.Model
            : config.Ollama.Model;
        var contextWindowTokens = config.DefaultProvider == ProviderType.Cerebras
            ? await TryResolveCerebrasContextWindowAsync(config, activeModel, ct)
            : null;
        await ApiResponse.WriteJsonAsync(response, new
        {
            provider          = config.DefaultProvider.ToString().ToLower(),
            model             = activeModel,
            contextWindowTokens,
            hasApiKey         = config.Cerebras.ApiKey is not null,
            apiKey            = config.Cerebras.ApiKey,
            ollamaHost        = config.Ollama.BaseUrl,
            execMode          = ExecModeConverter.ToString(state.ExecMode),
            forceTools        = state.ForceTools,
            cerebras          = new
            {
                model = config.Cerebras.Model,
                apiKey = config.Cerebras.ApiKey,
                hasApiKey = config.Cerebras.ApiKey is not null,
                baseUrl = config.Cerebras.BaseUrl,
                temperature = config.Cerebras.Temperature,
                topP = config.Cerebras.TopP,
                maxCompletionTokens = config.Cerebras.MaxCompletionTokens,
                seed = config.Cerebras.Seed,
                reasoningEffort = config.Cerebras.ReasoningEffort,
            },
            ollama            = new
            {
                model = config.Ollama.Model,
                host = config.Ollama.BaseUrl,
                temperature = config.Ollama.Temperature,
                topP = config.Ollama.TopP,
                seed = config.Ollama.Seed,
            },
        });
    }

    // ── PUT /api/settings ───────────────────────────────────────────────────

    private async Task HandlePutAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
            return;
        }
        var body = await JsonSerializer.DeserializeAsync<SettingsRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Ungültiger Request-Body." }, statusCode: 400);
            return;
        }
        var config = await configService.LoadAsync();
        var providerType = ParseProviderType(body.Provider);
        if (providerType is not null) config.DefaultProvider = providerType.Value;

        if (body.Cerebras is not null)
        {
            if (body.Cerebras.Model is not null) config.Cerebras.Model = body.Cerebras.Model;
            if (!string.IsNullOrWhiteSpace(body.Cerebras.ApiKey)) config.Cerebras.ApiKey = body.Cerebras.ApiKey;
            if (body.Cerebras.BaseUrl is not null) config.Cerebras.BaseUrl = body.Cerebras.BaseUrl;
            if (body.Cerebras.Temperature is not null) config.Cerebras.Temperature = body.Cerebras.Temperature;
            if (body.Cerebras.TopP is not null) config.Cerebras.TopP = body.Cerebras.TopP;
            if (body.Cerebras.MaxCompletionTokens is not null) config.Cerebras.MaxCompletionTokens = body.Cerebras.MaxCompletionTokens;
            if (body.Cerebras.Seed is not null) config.Cerebras.Seed = body.Cerebras.Seed;
            if (body.Cerebras.ReasoningEffort is not null) config.Cerebras.ReasoningEffort = body.Cerebras.ReasoningEffort;
        }

        if (body.Ollama is not null)
        {
            if (body.Ollama.Model is not null) config.Ollama.Model = body.Ollama.Model;
            if (body.Ollama.Host is not null) config.Ollama.BaseUrl = body.Ollama.Host;
            if (body.Ollama.Temperature is not null) config.Ollama.Temperature = body.Ollama.Temperature;
            if (body.Ollama.TopP is not null) config.Ollama.TopP = body.Ollama.TopP;
            if (body.Ollama.Seed is not null) config.Ollama.Seed = body.Ollama.Seed;
        }

        if (body.Model is not null)
        {
            if (config.DefaultProvider == ProviderType.Cerebras) config.Cerebras.Model = body.Model;
            else config.Ollama.Model = body.Model;
        }
        if (!string.IsNullOrEmpty(body.ApiKey)) config.Cerebras.ApiKey = body.ApiKey;
        if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;
        if (body.ExecMode is not null) state.ExecMode = ExecModeConverter.Parse(body.ExecMode) ?? state.ExecMode;
        if (body.ForceTools is bool ft) state.ForceTools = ft;
        await configService.SaveAsync(config);
        await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
    }

    // ── POST /api/settings/test ─────────────────────────────────────────────

    private async Task HandleTestAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
            return;
        }
        var config = await configService.LoadAsync();
        var provider = ProviderFactory.Create(config);
        var sw = Stopwatch.StartNew();
        try
        {
            await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")], ct);
            sw.Stop();
            await ApiResponse.WriteJsonAsync(response, new { ok = true, latencyMs = (int)sw.ElapsedMilliseconds });
        }
        catch (LlmProviderException ex)
        {
            await ApiResponse.WriteJsonAsync(response, new { ok = false, error = ex.Message });
        }
    }

    // ── Cerebras context-window cache ───────────────────────────────────────

    private static async Task<int?> TryResolveCerebrasContextWindowAsync(AppConfig config, string model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model)) return null;

        lock (ContextWindowCacheLock)
        {
            if (ContextWindowCache.TryGetValue(model, out var cached)
                && DateTime.UtcNow - cached.fetchedAtUtc <= ContextWindowCacheTtl)
                return cached.value;
        }

        int? resolved = null;
        try
        {
            var baseUri = TryGetCerebrasApiRoot(config.Cerebras.BaseUrl);
            var modelId  = Uri.EscapeDataString(model);
            var url      = $"{baseUri}/public/v1/models/{modelId}?format=huggingface";
            using var response = await MetadataHttp.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                lock (ContextWindowCacheLock) ContextWindowCache[model] = (DateTime.UtcNow, null);
                return null;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var payload = await JsonSerializer.DeserializeAsync<CerebrasModelMetadata>(stream, JsonDefaults.Options, ct);
            resolved = payload?.ContextLength;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                      or TaskCanceledException or OperationCanceledException)
        {
            // Kein Hard-Fail: Settings-Endpoint bleibt nutzbar.
            _ = ex;
        }

        lock (ContextWindowCacheLock) ContextWindowCache[model] = (DateTime.UtcNow, resolved);
        return resolved;
    }

    private static string TryGetCerebrasApiRoot(string? configuredBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl)) return "https://api.cerebras.ai";
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri))
            return $"{uri.Scheme}://{uri.Authority}";
        return "https://api.cerebras.ai";
    }

    // ── Hilfsmethoden ───────────────────────────────────────────────────────

    private static ProviderType? ParseProviderType(string? provider) =>
        provider?.ToLowerInvariant() switch
        {
            "ollama"   => ProviderType.Ollama,
            "cerebras" => ProviderType.Cerebras,
            _          => null
        };

    private sealed record SettingsRequest(
        string? Provider, string? Model, string? ApiKey,
        string? OllamaHost, string? ExecMode, bool? ForceTools,
        ProviderConfigRequest? Cerebras, ProviderConfigRequest? Ollama);

    private sealed record ProviderConfigRequest(
        string? Model,
        string? ApiKey,
        string? BaseUrl,
        string? Host,
        double? Temperature,
        double? TopP,
        int? MaxCompletionTokens,
        int? Seed,
        string? ReasoningEffort);

    private sealed class CerebrasModelMetadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
    }
}
