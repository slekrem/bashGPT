using System.Diagnostics;
using Microsoft.Extensions.Logging;
using bashGPT.Core;
using bashGPT.Server.Extensions;

const string Url = "http://127.0.0.1:5050";

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
builder.WebHost.UseUrls(Url);
builder.WebHost.UseKestrel(o => o.AllowSynchronousIO = true);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddBashGptServer(
    configService, toolRegistry,
    agentRegistry, sessionStore, sessionRequestStore);

var app = builder.Build();
app.UseBashGptPipeline();

Console.WriteLine($"bashGPT Server running on {Url}/");
Console.WriteLine("Press Ctrl+C to stop.");

TryOpenBrowser($"{Url}/");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await app.RunAsync(cts.Token);

static void TryOpenBrowser(string url)
{
    try
    {
        if (OperatingSystem.IsMacOS()) { Process.Start("open", url); return; }
        if (OperatingSystem.IsWindows()) { Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true }); return; }
        if (OperatingSystem.IsLinux()) Process.Start("xdg-open", url);
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
    {
        _ = ex;
    }
}

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
