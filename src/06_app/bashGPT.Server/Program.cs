using System.Net;
using bashGPT.Core;
using bashGPT.Server.Extensions;

// Startup logger: used before the DI container is built (plugin loading, registry setup).
// Disposed once the app is fully configured.
using var startupLoggerFactory = LoggerFactory.Create(b =>
    b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var startupLogger = startupLoggerFactory.CreateLogger("bashGPT.Server.Startup");

var configService = ServerApplication.CreateConfigurationService();
var pluginResult = ServerApplication.LoadPlugins(logger: startupLogger);
var toolRegistry = ServerApplication.CreateToolRegistry(pluginResult.Tools, startupLogger);
var agentRegistry = ServerApplication.CreateAgentRegistry(pluginResult.Agents, startupLogger);
var sessionStore = AppBootstrap.CreateSessionStore();
var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory,
});

var url = builder.Configuration.GetValue<string>("Server:Url") ?? "http://127.0.0.1:5050";
var uri = new Uri(url);

if (!IPAddress.TryParse(uri.Host, out var listenAddress))
    listenAddress = IPAddress.Loopback;

builder.WebHost.ConfigureKestrel(o =>
{
    o.Listen(listenAddress, uri.Port);
    o.AllowSynchronousIO = true;
});
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddBashGptServer(
    configService, toolRegistry,
    agentRegistry, sessionStore, sessionRequestStore);

var app = builder.Build();
app.UseBashGptPipeline();

app.Logger.LogInformation("bashGPT Server running on {Url}/", url);
app.Logger.LogInformation("Press Ctrl+C to stop.");

await app.RunAsync();

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
