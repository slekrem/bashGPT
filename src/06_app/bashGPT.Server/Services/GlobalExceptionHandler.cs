using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Services;

/// <summary>
/// Catches unhandled exceptions, logs them with full context, and returns a
/// RFC 7807 Problem Details response so clients always get a consistent error shape.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        // codeql[cs/log-forging] - Path is sanitized (CR/LF stripped); Serilog structured logging parameterizes values
        logger.LogError(exception, "Unhandled exception at {Method} {Path}",
            ctx.Request.Method, ctx.Request.Path.Value?.Replace('\r', ' ').Replace('\n', ' '));

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = ApiErrors.GenericServerError,
        }, ct);

        return true;
    }
}
