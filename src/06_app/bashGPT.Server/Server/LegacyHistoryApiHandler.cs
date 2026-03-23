using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class LegacyHistoryApiHandler(SessionStore? sessionStore)
{
    public async Task HandleHistoryAsync(HttpContext ctx, CancellationToken ct)
    {
        if (sessionStore is null)
        {
            await ctx.Response.WriteJsonAsync(new { history = Array.Empty<object>() });
            return;
        }

        var sessions = await sessionStore.LoadAllAsync();
        var latest = sessions
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            await ctx.Response.WriteJsonAsync(new { history = Array.Empty<object>() });
            return;
        }

        var session = await sessionStore.LoadAsync(latest.Id);
        var history = session?.Messages
            .Where(m =>
                m.Role == "user"
                || m.Role == "tool"
                || (m.Role == "assistant" && (!string.IsNullOrEmpty(m.Content) || m.ToolCalls is { Count: > 0 })))
            .Select(m => new { role = m.Role, content = m.Content ?? string.Empty })
            .ToList()
            ?? [];

        await ctx.Response.WriteJsonAsync(new { history });
    }

    public async Task HandleResetAsync(HttpContext ctx, CancellationToken ct)
    {
        if (sessionStore is not null)
            await sessionStore.ClearAsync();

        await ctx.Response.WriteJsonAsync(new { ok = true });
    }
}
