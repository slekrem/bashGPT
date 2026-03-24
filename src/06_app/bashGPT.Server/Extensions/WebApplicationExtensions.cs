namespace bashGPT.Server.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication UseBashGptPipeline(this WebApplication app)
    {
        // GlobalExceptionHandler (IExceptionHandler) handles unhandled exceptions:
        // structured logging + RFC 7807 Problem Details response.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapFallback("/api/{**path}", ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return ctx.Response.WriteAsJsonAsync(new { error = "Not found." });
        });
        app.MapFallbackToFile("index.html");

        return app;
    }
}
