using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Server;
using bashGPT.Agents;
using bashGPT.Tools.Registration;

namespace bashGPT.Server.Tests;

internal static class TestServerFactory
{
    public static async Task<(WebApplication App, HttpClient Client)> CreateAsync(
        IChatHandler handler,
        SessionStore? sessionStore = null,
        ConfigurationService? configService = null,
        AgentRegistry? agentRegistry = null,
        ToolRegistry? toolRegistry = null)
    {
        var port = GetFreePort();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.WebHost.UseKestrel(o => o.AllowSynchronousIO = true);
        builder.Logging.ClearProviders();

        var options = new ServerOptions(Port: port, NoBrowser: true, Model: null, Verbose: false);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IChatHandler>(handler);
        builder.Services.AddSingleton<RunningChatRegistry>();
        if (sessionStore is not null) builder.Services.AddSingleton(sessionStore);
        if (configService is not null) builder.Services.AddSingleton(configService);
        if (agentRegistry is not null) builder.Services.AddSingleton(agentRegistry);
        if (toolRegistry is not null) builder.Services.AddSingleton(toolRegistry);

        builder.Services.AddSingleton<VersionApiHandler>();
        builder.Services.AddSingleton(sp => new SettingsApiHandler(sp.GetService<ConfigurationService>()));
        builder.Services.AddSingleton(sp => new SessionApiHandler(sp.GetService<SessionStore>()));
        builder.Services.AddSingleton(sp => new AgentApiHandler(sp.GetService<AgentRegistry>(), sp.GetService<ConfigurationService>()));
        builder.Services.AddSingleton(sp => new ToolApiHandler(sp.GetService<ToolRegistry>()));
        builder.Services.AddSingleton(sp => new ChatApiHandler(
            sp.GetRequiredService<IChatHandler>(),
            sp.GetRequiredService<ServerOptions>(),
            sp.GetService<SessionStore>(),
            sp.GetService<SessionRequestStore>(),
            sp.GetService<ToolRegistry>(),
            sp.GetService<AgentRegistry>()));
        builder.Services.AddSingleton(sp => new StreamingChatApiHandler(
            sp.GetRequiredService<IChatHandler>(),
            sp.GetRequiredService<ServerOptions>(),
            sp.GetRequiredService<RunningChatRegistry>(),
            sp.GetService<SessionStore>(),
            sp.GetService<SessionRequestStore>(),
            sp.GetService<ToolRegistry>(),
            sp.GetService<AgentRegistry>()));
        builder.Services.AddSingleton<ChatCancelApiHandler>();
        builder.Services.AddSingleton(sp => new LegacyHistoryApiHandler(sp.GetService<SessionStore>()));

        var app = builder.Build();
        new WebApplicationHost(app).MapRoutes();

        await app.StartAsync();

        var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        return (app, client);
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
