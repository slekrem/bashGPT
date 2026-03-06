using BashGPT.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BashGPT.Providers;

public static class ProviderFactory
{
    /// <summary>
    /// Erstellt den konfigurierten Provider. Ein optionaler <paramref name="overrideType"/>
    /// (z. B. aus einem CLI-Flag) hat Vorrang vor der Config.
    /// </summary>
    public static ILlmProvider Create(
        AppConfig config,
        ProviderType? overrideType = null,
        LlmRateLimiter? sharedLimiter = null)
    {
        var type = overrideType ?? config.DefaultProvider;

        ILlmProvider provider = type switch
        {
            ProviderType.Ollama   => new OllamaProvider(config.Ollama),
            ProviderType.Cerebras => new CerebrasProvider(config.Cerebras),
            _                     => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (config.RateLimiting.Enabled)
        {
            var limiter = sharedLimiter ?? new LlmRateLimiter(
                config.RateLimiting.MaxRequestsPerMinute,
                config.RateLimiting.AgentRequestDelayMs);
            provider = new RateLimitedLlmProvider(provider, limiter);
        }

        return provider;
    }

    /// <summary>
    /// Registriert alle Provider-Dienste im DI-Container.
    /// </summary>
    public static IServiceCollection AddBashGptProviders(
        this IServiceCollection services,
        AppConfig config,
        ProviderType? overrideType = null)
    {
        services.AddHttpClient();
        services.AddSingleton(config);
        services.AddSingleton<ILlmProvider>(_ => Create(config, overrideType));
        return services;
    }
}
