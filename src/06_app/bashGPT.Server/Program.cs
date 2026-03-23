using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using bashGPT.Agents;
using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Tools.Registration;
using bashGPT.Server;
using bashGPT.Server.Controllers;

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

    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://127.0.0.1:{serverOptions.Port}");
    builder.WebHost.UseKestrel(o => o.AllowSynchronousIO = true);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    builder.Services.ConfigureHttpJsonOptions(opts =>
    {
        opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.SerializerOptions.PropertyNameCaseInsensitive = true;
    });
    builder.Services.AddControllers().AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
    builder.Services.AddSingleton<IControllerActivator, SingletonControllerActivator>();

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
    builder.Services.AddSingleton(sp => new ServerSessionService(
        sp.GetService<SessionStore>(),
        sp.GetService<SessionRequestStore>()));

    // Controllers — factory delegates for optional dependencies
    builder.Services.AddSingleton<VersionController>();
    builder.Services.AddSingleton(sp => new SettingsController(sp.GetService<ConfigurationService>()));
    builder.Services.AddSingleton(sp => new SessionsController(sp.GetService<SessionStore>()));
    builder.Services.AddSingleton(sp => new AgentsController(sp.GetService<AgentRegistry>(), sp.GetService<ConfigurationService>()));
    builder.Services.AddSingleton(sp => new ToolsController(sp.GetService<ToolRegistry>()));
    builder.Services.AddSingleton(sp => new LegacyController(sp.GetService<SessionStore>()));
    builder.Services.AddSingleton(sp => new ChatController(
        sp.GetRequiredService<IChatHandler>(),
        sp.GetRequiredService<ServerOptions>(),
        sp.GetRequiredService<RunningChatRegistry>(),
        sp.GetRequiredService<ServerSessionService>(),
        sp.GetService<ToolRegistry>(),
        sp.GetService<AgentRegistry>()));

    var app = builder.Build();

    app.UseExceptionHandler(exceptionApp =>
        exceptionApp.Run(async ctx =>
        {
            var feature = ctx.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error is not null)
                Console.Error.WriteLine($"[server] Unhandled request error: {feature.Error}");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new { error = ApiErrors.GenericServerError });
        }));

    app.UseStaticFiles();
    app.MapControllers();

    // Frontend (embedded resources, fallback when wwwroot files are absent)
    app.MapGet("/", ServeEmbeddedAsync("bashGPT.Web.index.html", "text/html; charset=utf-8",
        "<!doctype html><html><head><meta charset=\"utf-8\"><title>bashGPT</title></head><body><div id=\"app\"></div><script src=\"/bundle.js\"></script></body></html>"));
    app.MapGet("/bundle.js", ServeEmbeddedAsync("bashGPT.Web.bundle.js", "application/javascript; charset=utf-8",
        "console.warn('bashGPT frontend bundle not embedded.');"));

    Console.WriteLine($"bashGPT Server running on http://127.0.0.1:{serverOptions.Port}/");
    Console.WriteLine("Press Ctrl+C to stop.");

    if (!serverOptions.NoBrowser)
        TryOpenBrowser($"http://127.0.0.1:{serverOptions.Port}/");

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await app.RunAsync(cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();

static RequestDelegate ServeEmbeddedAsync(string resourceName, string contentType, string fallback)
    => async ctx =>
    {
        var stream = GetResourceStream(resourceName);
        if (stream is null)
        {
            ctx.Response.ContentType = contentType;
            await ctx.Response.WriteAsync(fallback);
            return;
        }
        using (stream)
        {
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength = stream.Length;
            await stream.CopyToAsync(ctx.Response.Body);
        }
    };

static Stream? GetResourceStream(string name)
{
    foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() })
    {
        if (assembly is null) continue;
        var stream = assembly.GetManifestResourceStream(name);
        if (stream is not null) return stream;
    }
    return null;
}

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
