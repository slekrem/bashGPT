using System.Diagnostics;
using System.Net;
using System.Text.Json;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers.Ollama;
using bashGPT.Core.Serialization;

namespace bashGPT.Server;

internal sealed class SettingsApiHandler(ConfigurationService? configService)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";

        if (ctx.Request.HttpMethod == "GET" && path == "/api/settings")
        { await HandleGetAsync(ctx.Response, ct); return; }

        if (ctx.Request.HttpMethod == "PUT" && path == "/api/settings")
        { await HandlePutAsync(ctx, ct); return; }

        if (ctx.Request.HttpMethod == "POST" && path == "/api/settings/test")
        { await HandleTestAsync(ctx.Response, ct); return; }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Not found." }, statusCode: 404);
    }

    private async Task HandleGetAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var config = await configService.LoadAsync();
        await ApiResponse.WriteJsonAsync(response, new
        {
            provider = "ollama",
            model = config.Ollama.Model,
            contextWindowTokens = (int?)null,
            ollamaHost = config.Ollama.BaseUrl,
            ollama = new
            {
                model = config.Ollama.Model,
                host = config.Ollama.BaseUrl,
            },
        });
    }

    private async Task HandlePutAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var body = await JsonSerializer.DeserializeAsync<SettingsRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Invalid request body." }, statusCode: 400);
            return;
        }

        var config = await configService.LoadAsync();

        if (body.Ollama is not null)
        {
            if (body.Ollama.Model is not null) config.Ollama.Model = body.Ollama.Model;
            if (body.Ollama.Host is not null) config.Ollama.BaseUrl = body.Ollama.Host;
        }

        if (body.Model is not null) config.Ollama.Model = body.Model;
        if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;

        await configService.SaveAsync(config);
        await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
    }

    private async Task HandleTestAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var config = await configService.LoadAsync();
        var provider = new OllamaProvider(config.Ollama);
        var sw = Stopwatch.StartNew();
        try
        {
            await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")], ct);
            sw.Stop();
            await ApiResponse.WriteJsonAsync(response, new { ok = true, latencyMs = (int)sw.ElapsedMilliseconds });
        }
        catch (LlmProviderException ex)
        {
            Console.Error.WriteLine($"[server] Provider connectivity test failed: {ex}");
            await ApiResponse.WriteJsonAsync(response, new { ok = false, error = ApiErrors.GenericProviderError });
        }
    }

    private sealed record SettingsRequest(
        string? Provider,
        string? Model,
        string? OllamaHost,
        ProviderConfigRequest? Ollama);

    private sealed record ProviderConfigRequest(
        string? Model,
        string? Host);
}
