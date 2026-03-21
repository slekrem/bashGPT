using bashGPT.Core.Configuration;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers.Ollama;

namespace bashGPT.Core.Providers;

public static class LlmProviderBootstrap
{
    public static async Task<(AppConfig? Config, ILlmProvider? Provider, string? Error)> CreateAsync(
        ConfigurationService configService,
        string? modelOverride,
        ILlmProvider? providerOverride = null)
    {
        if (providerOverride is not null)
            return (null, providerOverride, null);

        AppConfig config;
        try
        {
            config = await configService.LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            return (null, null, $"Configuration error: {ex.Message}");
        }

        Chat.ChatOrchestrator.ApplyModelOverride(config, modelOverride);

        try
        {
            return (config, new OllamaProvider(config.Ollama), null);
        }
        catch (Exception ex)
        {
            return (config, null, $"Provider error: {ex.Message}");
        }
    }
}
