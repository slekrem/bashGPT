using System.Net;
using bashGPT.Core;
using bashGPT.Server.Extensions;
using Serilog;
using Serilog.Events;

var logsDir = AppBootstrap.GetLogsDir();
Directory.CreateDirectory(logsDir);

// Bootstrap logger: used before the DI container is built (plugin loading, registry setup).
// Replaced by the full Serilog configuration once the host is built.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File(
        Path.Combine(logsDir, "server-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateBootstrapLogger();

using var startupLoggerFactory = LoggerFactory.Create(b => b.AddSerilog());
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
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.File(
        Path.Combine(logsDir, "server-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        restrictedToMinimumLevel: LogEventLevel.Warning));

builder.Services.AddBashGptServer(
    configService, toolRegistry,
    agentRegistry, sessionStore, sessionRequestStore);

var app = builder.Build();
app.UseBashGptPipeline();

app.Logger.LogInformation("bashGPT Server running on {Url}/", url);
app.Logger.LogInformation("Press Ctrl+C to stop.");

try
{
    await app.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
