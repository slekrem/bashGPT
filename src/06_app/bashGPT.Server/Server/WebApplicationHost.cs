using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;

namespace bashGPT.Server;

internal sealed class WebApplicationHost(WebApplication app)
{
    public void MapRoutes()
    {
        app.UseExceptionHandler(exceptionApp =>
            exceptionApp.Run(async ctx =>
            {
                var feature = ctx.Features.Get<IExceptionHandlerPathFeature>();
                if (feature?.Error is not null)
                    Console.Error.WriteLine($"[server] Unhandled request error: {feature.Error}");
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsJsonAsync(new { error = ApiErrors.GenericServerError });
            }));

        app.MapGet("/", ServeIndexHtmlAsync);
        app.MapGet("/bundle.js", ServeBundleJsAsync);

        app.MapGet("/api/version",
            (VersionApiHandler h) => h.Get());
        app.MapGet("/api/history",
            (LegacyHistoryApiHandler h, CancellationToken ct) => h.GetHistoryAsync(ct));
        app.MapPost("/api/reset",
            (LegacyHistoryApiHandler h, CancellationToken ct) => h.ResetAsync(ct));

        app.MapGet("/api/settings",
            (SettingsApiHandler h, CancellationToken ct) => h.GetAsync(ct));
        app.MapPut("/api/settings",
            (SettingsApiHandler h, HttpRequest req, CancellationToken ct) => h.PutAsync(req, ct));
        app.MapPost("/api/settings/test",
            (SettingsApiHandler h, CancellationToken ct) => h.TestAsync(ct));

        app.MapPost("/api/chat/stream",
            (StreamingChatApiHandler h, HttpContext ctx, CancellationToken ct) => h.PostAsync(ctx, ct));
        app.MapPost("/api/chat/cancel",
            (ChatCancelApiHandler h, HttpRequest req, CancellationToken ct) => h.PostAsync(req, ct));
        app.MapPost("/api/chat",
            (ChatApiHandler h, HttpRequest req, CancellationToken ct) => h.PostAsync(req, ct));

        app.MapGet("/api/sessions",
            (SessionApiHandler h, CancellationToken ct) => h.GetAllAsync(ct));
        app.MapPost("/api/sessions",
            (SessionApiHandler h, CancellationToken ct) => h.CreateAsync(ct));
        app.MapPost("/api/sessions/clear",
            (SessionApiHandler h, CancellationToken ct) => h.ClearAsync(ct));
        app.MapGet("/api/sessions/{id}",
            (SessionApiHandler h, string id, CancellationToken ct) => h.GetByIdAsync(id, ct));
        app.MapPut("/api/sessions/{id}",
            (SessionApiHandler h, string id, HttpRequest req, CancellationToken ct) => h.PutAsync(id, req, ct));
        app.MapDelete("/api/sessions/{id}",
            (SessionApiHandler h, string id, CancellationToken ct) => h.DeleteAsync(id, ct));

        app.MapGet("/api/tools",
            (ToolApiHandler h) => h.Get());

        app.MapGet("/api/agents",
            (AgentApiHandler h) => h.GetAll());
        app.MapGet("/api/agents/{id}/info-panel",
            (AgentApiHandler h, string id, CancellationToken ct) => h.GetInfoPanel(id, ct));
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
