using System.Net;
using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class LegacyHistoryApiHandler(SessionStore? sessionStore)
{
    public async Task HandleHistoryAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (sessionStore is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { history = Array.Empty<object>() });
            return;
        }

        var sessions = await sessionStore.LoadAllAsync();
        var latest = sessions
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { history = Array.Empty<object>() });
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

        await ApiResponse.WriteJsonAsync(response, new { history });
    }

    public async Task HandleResetAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (sessionStore is not null)
            await sessionStore.ClearAsync();

        await ApiResponse.WriteJsonAsync(response, new { ok = true });
    }
}
