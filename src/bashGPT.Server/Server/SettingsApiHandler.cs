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
        await ApiResponse.WriteJsonAsync(response, new
        {
            provider          = "ollama",
            model             = config.Ollama.Model,
            contextWindowTokens = (int?)null,
            ollamaHost        = config.Ollama.BaseUrl,
            execMode          = ExecModeConverter.ToString(state.ExecMode),
            forceTools        = state.ForceTools,
            commandTimeoutSeconds = config.CommandTimeoutSeconds,
            loopDetectionEnabled = config.LoopDetectionEnabled,
            maxToolCallRounds    = config.MaxToolCallRounds,
            rateLimiting      = new
            {
                enabled              = config.RateLimiting.Enabled,
                maxRequestsPerMinute = config.RateLimiting.MaxRequestsPerMinute,
                agentRequestDelayMs  = config.RateLimiting.AgentRequestDelayMs,
            },
            ollama            = new
            {
                model = config.Ollama.Model,
                host = config.Ollama.BaseUrl,
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

        if (body.Ollama is not null)
        {
            if (body.Ollama.Model is not null) config.Ollama.Model = body.Ollama.Model;
            if (body.Ollama.Host is not null) config.Ollama.BaseUrl = body.Ollama.Host;
        }

        if (body.Model is not null) config.Ollama.Model = body.Model;
        if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;
        if (body.ExecMode is not null && ExecModeConverter.Parse(body.ExecMode) is { } mode)
        {
            state.ExecMode = mode;
            config.DefaultExecMode = mode;
        }
        if (body.ForceTools is bool ft)
        {
            state.ForceTools = ft;
            config.DefaultForceTools = ft;
        }
        if (body.CommandTimeoutSeconds is { } timeout && timeout > 0)
            config.CommandTimeoutSeconds = timeout;
        if (body.LoopDetectionEnabled is { } lde) config.LoopDetectionEnabled = lde;
        if (body.MaxToolCallRounds is { } mtr && mtr > 0) config.MaxToolCallRounds = mtr;
        if (body.RateLimiting is { } rl)
        {
            if (rl.Enabled is { } enabled) config.RateLimiting.Enabled = enabled;
            if (rl.MaxRequestsPerMinute is { } rpm && rpm > 0) config.RateLimiting.MaxRequestsPerMinute = rpm;
            if (rl.AgentRequestDelayMs is { } delay && delay >= 0) config.RateLimiting.AgentRequestDelayMs = delay;
        }
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

    // ── Hilfsmethoden ───────────────────────────────────────────────────────

    private static ProviderType? ParseProviderType(string? provider) =>
        provider?.ToLowerInvariant() switch
        {
            "ollama"   => ProviderType.Ollama,
            _          => null
        };

    private sealed record SettingsRequest(
        string? Provider, string? Model,
        string? OllamaHost, string? ExecMode, bool? ForceTools,
        int? CommandTimeoutSeconds, bool? LoopDetectionEnabled, int? MaxToolCallRounds,
        RateLimitingRequest? RateLimiting,
        ProviderConfigRequest? Ollama);

    private sealed record RateLimitingRequest(bool? Enabled, int? MaxRequestsPerMinute, int? AgentRequestDelayMs);

    private sealed record ProviderConfigRequest(
        string? Model,
        string? Host);
}
