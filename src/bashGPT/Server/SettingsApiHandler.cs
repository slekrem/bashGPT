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
            ollamaHost        = config.Ollama.BaseUrl,
            execMode          = ExecModeToString(state.ExecMode),
            forceTools        = state.ForceTools,
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
        if (body.Model is not null)
        {
            if (config.DefaultProvider == ProviderType.Cerebras) config.Cerebras.Model = body.Model;
            else config.Ollama.Model = body.Model;
        }
        if (!string.IsNullOrEmpty(body.ApiKey)) config.Cerebras.ApiKey = body.ApiKey;
        if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;
        if (body.ExecMode is not null) state.ExecMode = ParseExecMode(body.ExecMode) ?? state.ExecMode;
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

    internal static string ExecModeToString(ExecutionMode mode) =>
        mode switch
        {
            ExecutionMode.Ask      => "ask",
            ExecutionMode.DryRun   => "dry-run",
            ExecutionMode.AutoExec => "auto-exec",
            ExecutionMode.NoExec   => "no-exec",
            _                      => "ask"
        };

    internal static ExecutionMode? ParseExecMode(string? mode) =>
        mode?.ToLowerInvariant() switch
        {
            "ask"       => ExecutionMode.Ask,
            "dry-run"   => ExecutionMode.DryRun,
            "auto-exec" => ExecutionMode.AutoExec,
            "no-exec"   => ExecutionMode.NoExec,
            _           => null
        };

    private sealed record SettingsRequest(
        string? Provider, string? Model, string? ApiKey,
        string? OllamaHost, string? ExecMode, bool? ForceTools);

    private sealed class CerebrasModelMetadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
    }
}
