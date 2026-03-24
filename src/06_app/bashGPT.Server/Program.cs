using System.Diagnostics;
using System.Net;
using bashGPT.Core;
using bashGPT.Server.Extensions;

var configService = ServerApplication.CreateConfigurationService();
var pluginResult = ServerApplication.LoadPlugins();
var toolRegistry = ServerApplication.CreateToolRegistry(pluginResult.Tools);
var agentRegistry = ServerApplication.CreateAgentRegistry(pluginResult.Agents);
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

// Open the browser only after the server is actually listening
app.Lifetime.ApplicationStarted.Register(() => TryOpenBrowser($"{url}/", app.Logger));

await app.RunAsync();

static void TryOpenBrowser(string url, ILogger logger)
{
    try
    {
        if (Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }) is null)
            logger.LogWarning("Could not open browser for {Url}.", url);
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
    {
        logger.LogWarning("Could not open browser: {Message}", ex.Message);
    }
}

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
