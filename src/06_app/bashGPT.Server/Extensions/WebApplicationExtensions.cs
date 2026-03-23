using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;

namespace bashGPT.Server.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication UseBashGptPipeline(this WebApplication app)
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

        app.UseStaticFiles();
        app.MapControllers();

        app.MapGet("/", ServeEmbeddedAsync("bashGPT.Web.index.html", "text/html; charset=utf-8",
            "<!doctype html><html><head><meta charset=\"utf-8\"><title>bashGPT</title></head><body><div id=\"app\"></div><script src=\"/bundle.js\"></script></body></html>"));
        app.MapGet("/bundle.js", ServeEmbeddedAsync("bashGPT.Web.bundle.js", "application/javascript; charset=utf-8",
            "console.warn('bashGPT frontend bundle not embedded.');"));

        return app;
    }

    private static RequestDelegate ServeEmbeddedAsync(string resourceName, string contentType, string fallback)
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
