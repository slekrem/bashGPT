using System.Diagnostics;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers.Ollama;
using bashGPT.Core.Serialization;

namespace bashGPT.Server;

internal sealed class SettingsApiHandler(ConfigurationService? configService)
{
    public async Task GetAsync(HttpResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await response.WriteJsonAsync(new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var config = await configService.LoadAsync();
        await response.WriteJsonAsync(new
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

    public async Task PutAsync(HttpContext ctx, CancellationToken ct)
    {
        if (configService is null)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var body = await ctx.Request.ReadFromJsonAsync<SettingsRequest>(JsonDefaults.Options, ct);
        if (body is null)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Invalid request body." }, statusCode: 400);
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
        await ctx.Response.WriteJsonAsync(new { ok = true });
    }

    public async Task TestAsync(HttpResponse response, CancellationToken ct)
    {
        if (configService is null)
        {
            await response.WriteJsonAsync(new { error = "Configuration service is unavailable." }, statusCode: 503);
            return;
        }

        var config = await configService.LoadAsync();
        var provider = new OllamaProvider(config.Ollama);
        var sw = Stopwatch.StartNew();
        try
        {
            await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")], ct);
            sw.Stop();
            await response.WriteJsonAsync(new { ok = true, latencyMs = (int)sw.ElapsedMilliseconds });
        }
        catch (LlmProviderException ex)
        {
            Console.Error.WriteLine($"[server] Provider connectivity test failed: {ex}");
            await response.WriteJsonAsync(new { ok = false, error = ApiErrors.GenericProviderError });
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
