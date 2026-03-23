using System.CommandLine;
using Microsoft.Extensions.Logging;
using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Agents;
using bashGPT.Tools.Registration;
using bashGPT.Server;

var configService = ServerApplication.CreateConfigurationService();
var pluginResult = ServerApplication.LoadPlugins();
var toolRegistry = ServerApplication.CreateToolRegistry(pluginResult.Tools);

var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Model name (overrides config)"
};
var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Show debug output"
};
var portOpt = new Option<int>("--port")
{
    Description = "Port for server mode",
    DefaultValueFactory = _ => 5050
};
var noBrowserOpt = new Option<bool>("--no-browser")
{
    Description = "Do not open the browser automatically on startup"
};

var rootCommand = new RootCommand("bashGPT Server");
rootCommand.Options.Add(modelOpt);
rootCommand.Options.Add(verboseOpt);
rootCommand.Options.Add(portOpt);
rootCommand.Options.Add(noBrowserOpt);

rootCommand.SetAction(async (parseResult, ct) =>
{
    var serverOptions = new ServerOptions(
        Port: parseResult.GetValue(portOpt),
        NoBrowser: parseResult.GetValue(noBrowserOpt),
        Model: parseResult.GetValue(modelOpt),
        Verbose: parseResult.GetValue(verboseOpt));

    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://127.0.0.1:{serverOptions.Port}");
    builder.WebHost.UseKestrel(o => o.AllowSynchronousIO = true);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    builder.Services.ConfigureHttpJsonOptions(opts =>
    {
        opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

    var agentRegistry = ServerApplication.CreateAgentRegistry(pluginResult.Agents);
    var sessionStore = AppBootstrap.CreateSessionStore();
    var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();

    builder.Services.AddSingleton(serverOptions);
    builder.Services.AddSingleton(configService);
    builder.Services.AddSingleton(toolRegistry);
    builder.Services.AddSingleton(agentRegistry);
    builder.Services.AddSingleton(sessionStore);
    builder.Services.AddSingleton(sessionRequestStore);
    builder.Services.AddSingleton<IChatHandler>(sp => new ServerChatRunner(
        sp.GetRequiredService<ConfigurationService>(),
        toolRegistry: sp.GetRequiredService<ToolRegistry>()));
    builder.Services.AddSingleton<RunningChatRegistry>();

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

    Console.WriteLine($"bashGPT Server running on http://127.0.0.1:{serverOptions.Port}/");
    Console.WriteLine("Press Ctrl+C to stop.");

    if (!serverOptions.NoBrowser)
        WebApplicationHost.TryOpenBrowser($"http://127.0.0.1:{serverOptions.Port}/");

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await app.RunAsync(cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
