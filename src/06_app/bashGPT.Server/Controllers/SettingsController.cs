using System.Diagnostics;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers.Ollama;
using bashGPT.Server.Models;
using bashGPT.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(
    ConfigurationService? configService = null,
    ILogger<SettingsController>? logger = null) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (configService is null)
            return StatusCode(503, new { error = "Configuration service is unavailable." });

        var config = await configService.LoadAsync();
        return Ok(new
        {
            provider = "ollama",
            model = config.Ollama.Model,
            contextWindowTokens = (int?)null,
            ollamaHost = config.Ollama.BaseUrl,
            ollama = new { model = config.Ollama.Model, host = config.Ollama.BaseUrl },
        });
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] SettingsRequest body, CancellationToken ct)
    {
        if (configService is null)
            return StatusCode(503, new { error = "Configuration service is unavailable." });

        var config = await configService.LoadAsync();

        if (body.Ollama is not null)
        {
            if (body.Ollama.Model is not null) config.Ollama.Model = body.Ollama.Model;
            if (body.Ollama.Host is not null) config.Ollama.BaseUrl = body.Ollama.Host;
        }

        if (body.Model is not null) config.Ollama.Model = body.Model;
        if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;

        await configService.SaveAsync(config);
        return Ok(new { ok = true });
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        if (configService is null)
            return StatusCode(503, new { error = "Configuration service is unavailable." });

        var config = await configService.LoadAsync();
        var provider = new OllamaProvider(config.Ollama);
        var sw = Stopwatch.StartNew();
        try
        {
            await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")], ct);
            sw.Stop();
            return Ok(new { ok = true, latencyMs = (int)sw.ElapsedMilliseconds });
        }
        catch (LlmProviderException ex)
        {
            logger?.LogError(ex, "Provider connectivity test failed");
            return Ok(new { ok = false, error = ApiErrors.GenericProviderError });
        }
    }
}
