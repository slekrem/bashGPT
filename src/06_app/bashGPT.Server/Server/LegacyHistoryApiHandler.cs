using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class LegacyHistoryApiHandler(SessionStore? sessionStore)
{
    public async Task<IResult> GetHistoryAsync(CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { history = Array.Empty<object>() });

        var sessions = await sessionStore.LoadAllAsync();
        var latest = sessions.OrderByDescending(s => s.UpdatedAt).FirstOrDefault();

        if (latest is null)
            return Results.Json(new { history = Array.Empty<object>() });

        var session = await sessionStore.LoadAsync(latest.Id);
        var history = session?.Messages
            .Where(m =>
                m.Role == "user"
                || m.Role == "tool"
                || (m.Role == "assistant" && (!string.IsNullOrEmpty(m.Content) || m.ToolCalls is { Count: > 0 })))
            .Select(m => new { role = m.Role, content = m.Content ?? string.Empty })
            .ToList()
            ?? [];

        return Results.Json(new { history });
    }

    public async Task<IResult> ResetAsync(CancellationToken ct)
    {
        if (sessionStore is not null)
            await sessionStore.ClearAsync();

        return Results.Json(new { ok = true });
    }
}
