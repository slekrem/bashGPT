using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using bashGPT.Core;
using bashGPT.Server.Services;
using bashGPT.Server.Extensions;

var configService = ServerApplication.CreateConfigurationService();
var pluginResult = ServerApplication.LoadPlugins();
var toolRegistry = ServerApplication.CreateToolRegistry(pluginResult.Tools);

var modelOpt = new Option<string?>("--model", "-m") { Description = "Model name (overrides config)" };
var verboseOpt = new Option<bool>("--verbose", "-v") { Description = "Show debug output" };
var portOpt = new Option<int>("--port") { Description = "Port for server mode", DefaultValueFactory = _ => 5050 };
var noBrowserOpt = new Option<bool>("--no-browser") { Description = "Do not open the browser automatically on startup" };

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

    var agentRegistry = ServerApplication.CreateAgentRegistry(pluginResult.Agents);
    var sessionStore = AppBootstrap.CreateSessionStore();
    var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();

    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://127.0.0.1:{serverOptions.Port}");
    builder.WebHost.UseKestrel(o => o.AllowSynchronousIO = true);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    builder.Services.AddBashGptServer(
        serverOptions, configService, toolRegistry,
        agentRegistry, sessionStore, sessionRequestStore);

    var app = builder.Build();
    app.UseBashGptPipeline();

    Console.WriteLine($"bashGPT Server running on http://127.0.0.1:{serverOptions.Port}/");
    Console.WriteLine("Press Ctrl+C to stop.");

    if (!serverOptions.NoBrowser)
        TryOpenBrowser($"http://127.0.0.1:{serverOptions.Port}/");

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await app.RunAsync(cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();

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
