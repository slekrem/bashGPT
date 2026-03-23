using bashGPT.Core.Storage;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api")]
public sealed class LegacyController(SessionStore? sessionStore) : ControllerBase
{
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        if (sessionStore is null)
            return Ok(new { history = Array.Empty<object>() });

        var sessions = await sessionStore.LoadAllAsync();
        var latest = sessions.OrderByDescending(s => s.UpdatedAt).FirstOrDefault();

        if (latest is null)
            return Ok(new { history = Array.Empty<object>() });

        var session = await sessionStore.LoadAsync(latest.Id);
        var history = session?.Messages
            .Where(m =>
                m.Role == "user"
                || m.Role == "tool"
                || (m.Role == "assistant" && (!string.IsNullOrEmpty(m.Content) || m.ToolCalls is { Count: > 0 })))
            .Select(m => new { role = m.Role, content = m.Content ?? string.Empty })
            .ToList()
            ?? [];

        return Ok(new { history });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        if (sessionStore is not null)
            await sessionStore.ClearAsync();

        return Ok(new { ok = true });
    }
}
