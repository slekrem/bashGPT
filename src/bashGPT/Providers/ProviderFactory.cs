using BashGPT.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BashGPT.Providers;

public static class ProviderFactory
{
    /// <summary>
    /// Erstellt den konfigurierten Provider. Ein optionaler <paramref name="overrideType"/>
    /// (z. B. aus einem CLI-Flag) hat Vorrang vor der Config.
    /// </summary>
    public static ILlmProvider Create(AppConfig config, ProviderType? overrideType = null)
    {
        var type = overrideType ?? config.DefaultProvider;

        return type switch
        {
            ProviderType.Ollama   => new OllamaProvider(config.Ollama),
            ProviderType.Cerebras => new CerebrasProvider(config.Cerebras),
            _                     => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
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
