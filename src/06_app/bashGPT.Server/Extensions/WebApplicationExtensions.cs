using Microsoft.AspNetCore.Diagnostics;
using bashGPT.Server.Services;

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

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseStaticFiles();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        return app;
    }
}
