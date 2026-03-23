using System.Text.Json;
using bashGPT.Agents;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Tools.Registration;
using Microsoft.AspNetCore.Mvc.Controllers;
using bashGPT.Server.Controllers;

namespace bashGPT.Server.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBashGptServer(
        this IServiceCollection services,
        ServerOptions options,
        ConfigurationService configService,
        ToolRegistry toolRegistry,
        AgentRegistry agentRegistry,
        SessionStore sessionStore,
        SessionRequestStore sessionRequestStore)
    {
        services.AddOpenApi();

        services.AddSingleton(options);
        services.AddSingleton(configService);
        services.AddSingleton(toolRegistry);
        services.AddSingleton(agentRegistry);
        services.AddSingleton(sessionStore);
        services.AddSingleton(sessionRequestStore);
        services.AddSingleton<IChatHandler>(sp => new ServerChatRunner(
            sp.GetRequiredService<ConfigurationService>(),
            toolRegistry: sp.GetRequiredService<ToolRegistry>()));
        services.AddSingleton<RunningChatRegistry>();
        services.AddSingleton(sp => new ServerSessionService(
            sp.GetService<SessionStore>(),
            sp.GetService<SessionRequestStore>()));

        services.AddBashGptControllers();

        return services;
    }

    /// <summary>
    /// Registers JSON serialization, MVC controllers, the singleton controller activator,
    /// and all controller instances. Used by both the production host and test factories.
    /// </summary>
    public static IServiceCollection AddBashGptControllers(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.PropertyNameCaseInsensitive = true;
        });
        services.AddControllers()
            .AddApplicationPart(typeof(VersionController).Assembly)
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
        services.AddSingleton<IControllerActivator, SingletonControllerActivator>();

        // Factory delegates support optional (nullable) constructor dependencies
        services.AddSingleton<VersionController>();
        services.AddSingleton(sp => new SettingsController(sp.GetService<ConfigurationService>()));
        services.AddSingleton(sp => new SessionsController(sp.GetService<SessionStore>()));
        services.AddSingleton(sp => new AgentsController(sp.GetService<AgentRegistry>(), sp.GetService<ConfigurationService>()));
        services.AddSingleton(sp => new ToolsController(sp.GetService<ToolRegistry>()));
        services.AddSingleton(sp => new LegacyController(sp.GetService<SessionStore>()));
        services.AddSingleton(sp => new ChatController(
            sp.GetRequiredService<IChatHandler>(),
            sp.GetRequiredService<ServerOptions>(),
            sp.GetRequiredService<RunningChatRegistry>(),
            sp.GetRequiredService<ServerSessionService>(),
            sp.GetService<ToolRegistry>(),
            sp.GetService<AgentRegistry>()));

        return services;
    }
}
