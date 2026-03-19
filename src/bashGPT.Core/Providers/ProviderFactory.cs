using BashGPT.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BashGPT.Providers;

public static class ProviderFactory
{
    public static ILlmProvider Create(AppConfig config)
        => new OllamaProvider(config.Ollama);

    public static IServiceCollection AddBashGptProviders(
        this IServiceCollection services,
        AppConfig config)
    {
        services.AddHttpClient();
        services.AddSingleton(config);
        services.AddSingleton<ILlmProvider>(_ => Create(config));
        return services;
    }
}
