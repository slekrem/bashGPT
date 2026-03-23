using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Agents;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class WebApplicationHost
{
    private readonly WebApplication _app;
    private readonly ServerOptions _serverOptions;
    private readonly SettingsApiHandler _settingsHandler;
    private readonly ChatApiHandler _chatHandler;
    private readonly StreamingChatApiHandler _streamingChatHandler;
    private readonly ChatCancelApiHandler _chatCancelHandler;
    private readonly SessionApiHandler _sessionHandler;
    private readonly AgentApiHandler _agentHandler;
    private readonly ToolApiHandler _toolHandler;
    private readonly LegacyHistoryApiHandler _legacyHistoryHandler;

    public WebApplicationHost(WebApplication app)
    {
        _app = app;
        _serverOptions = app.Services.GetRequiredService<ServerOptions>();
        var runningChats = app.Services.GetRequiredService<RunningChatRegistry>();
        var handler = app.Services.GetRequiredService<IChatHandler>();
        var configService = app.Services.GetService<ConfigurationService>();
        var sessionStore = app.Services.GetService<SessionStore>();
        var sessionRequestStore = app.Services.GetService<SessionRequestStore>();
        var toolRegistry = app.Services.GetService<ToolRegistry>();
        var agentRegistry = app.Services.GetService<AgentRegistry>();

        _settingsHandler = new SettingsApiHandler(configService);
        _chatHandler = new ChatApiHandler(handler, sessionStore, sessionRequestStore, toolRegistry, agentRegistry);
        _streamingChatHandler = new StreamingChatApiHandler(handler, runningChats, sessionStore, sessionRequestStore, toolRegistry, agentRegistry);
        _chatCancelHandler = new ChatCancelApiHandler(runningChats);
        _sessionHandler = new SessionApiHandler(sessionStore);
        _agentHandler = new AgentApiHandler(agentRegistry, configService);
        _toolHandler = new ToolApiHandler(toolRegistry);
        _legacyHistoryHandler = new LegacyHistoryApiHandler(sessionStore);
    }

    public void MapRoutes()
    {
        _app.UseExceptionHandler(exceptionApp =>
            exceptionApp.Run(async ctx =>
            {
                var feature = ctx.Features.Get<IExceptionHandlerPathFeature>();
                if (feature?.Error is not null)
                    Console.Error.WriteLine($"[server] Unhandled request error: {feature.Error}");
                await ctx.Response.WriteJsonAsync(new { error = ApiErrors.GenericServerError }, statusCode: 500);
            }));

        _app.MapGet("/", ServeIndexHtmlAsync);
        _app.MapGet("/bundle.js", ServeBundleJsAsync);

        _app.MapGet("/api/version", (HttpContext ctx, CancellationToken ct) => new VersionApiHandler().HandleAsync(ctx, ct));
        _app.MapGet("/api/history", (HttpContext ctx, CancellationToken ct) => _legacyHistoryHandler.HandleHistoryAsync(ctx, ct));
        _app.MapPost("/api/reset", (HttpContext ctx, CancellationToken ct) => _legacyHistoryHandler.HandleResetAsync(ctx, ct));

        _app.MapGet("/api/settings", (HttpContext ctx, CancellationToken ct) => _settingsHandler.HandleAsync(ctx, ct));
        _app.MapPut("/api/settings", (HttpContext ctx, CancellationToken ct) => _settingsHandler.HandleAsync(ctx, ct));
        _app.MapPost("/api/settings/test", (HttpContext ctx, CancellationToken ct) => _settingsHandler.HandleAsync(ctx, ct));

        _app.MapPost("/api/chat/stream", (HttpContext ctx, CancellationToken ct) => _streamingChatHandler.HandleAsync(ctx, _serverOptions, ct));
        _app.MapPost("/api/chat/cancel", (HttpContext ctx, CancellationToken ct) => _chatCancelHandler.HandleAsync(ctx, ct));
        _app.MapPost("/api/chat", (HttpContext ctx, CancellationToken ct) => _chatHandler.HandleAsync(ctx, _serverOptions, ct));

        _app.MapGet("/api/sessions", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));
        _app.MapPost("/api/sessions", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));
        _app.MapPost("/api/sessions/clear", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));
        _app.MapGet("/api/sessions/{id}", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));
        _app.MapPut("/api/sessions/{id}", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));
        _app.MapDelete("/api/sessions/{id}", (HttpContext ctx, CancellationToken ct) => _sessionHandler.HandleAsync(ctx, ct));

        _app.MapGet("/api/tools", (HttpContext ctx, CancellationToken ct) => _toolHandler.HandleAsync(ctx, ct));

        _app.MapGet("/api/agents", (HttpContext ctx, CancellationToken ct) => _agentHandler.HandleAsync(ctx, ct));
        _app.MapGet("/api/agents/{id}/info-panel", (HttpContext ctx, CancellationToken ct) => _agentHandler.HandleAsync(ctx, ct));
    }

    internal static void TryOpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS()) { Process.Start("open", url); return; }
            if (OperatingSystem.IsWindows()) { Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true }); return; }
            if (OperatingSystem.IsLinux()) Process.Start("xdg-open", url);
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                      or System.ComponentModel.Win32Exception
                                      or IOException)
        {
            _ = ex;
        }
    }

    private static async Task ServeIndexHtmlAsync(HttpContext ctx)
    {
        var stream = GetResourceStream("bashGPT.Web.index.html");
        if (stream is null)
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync("<!doctype html><html><head><meta charset=\"utf-8\"><title>bashGPT</title></head><body><div id=\"app\"></div><script src=\"/bundle.js\"></script></body></html>");
            return;
        }
        using (stream)
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength = stream.Length;
            await stream.CopyToAsync(ctx.Response.Body);
        }
    }

    private static async Task ServeBundleJsAsync(HttpContext ctx)
    {
        var stream = GetResourceStream("bashGPT.Web.bundle.js");
        if (stream is null)
        {
            ctx.Response.ContentType = "application/javascript; charset=utf-8";
            await ctx.Response.WriteAsync("console.warn('bashGPT frontend bundle not embedded.');");
            return;
        }
        using (stream)
        {
            ctx.Response.ContentType = "application/javascript; charset=utf-8";
            ctx.Response.ContentLength = stream.Length;
            await stream.CopyToAsync(ctx.Response.Body);
        }
    }

    private static Stream? GetResourceStream(string name)
    {
        foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() })
        {
            if (assembly is null) continue;
            var stream = assembly.GetManifestResourceStream(name);
            if (stream is not null) return stream;
        }
        return null;
    }
}
