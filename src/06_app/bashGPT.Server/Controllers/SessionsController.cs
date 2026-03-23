using bashGPT.Core;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Storage;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/sessions")]
internal sealed class SessionsController(SessionStore? sessionStore) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        var sessions = await sessionStore.LoadAllAsync();
        return Ok(new
        {
            sessions = sessions.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                createdAt = s.CreatedAt,
                updatedAt = s.UpdatedAt,
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        var now = DateTime.UtcNow.ToString("o");
        var newSession = new SessionRecord
        {
            Id = $"{AppDefaults.SessionIdPrefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Title = "New Chat",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await sessionStore.UpsertAsync(newSession);
        return Ok(new
        {
            id = newSession.Id,
            title = newSession.Title,
            createdAt = newSession.CreatedAt,
            updatedAt = newSession.UpdatedAt,
        });
    }

    [HttpPost("clear")]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        await sessionStore.ClearAsync();
        return Ok(new { ok = true });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        var session = await sessionStore.LoadAsync(id);
        if (session is null) return NotFound(new { error = "Session not found." });

        var visibleMessages = session.Messages
            .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrEmpty(m.Content))
            .ToList();

        return Ok(new
        {
            id = session.Id,
            title = session.Title,
            createdAt = session.CreatedAt,
            updatedAt = session.UpdatedAt,
            messages = visibleMessages,
            enabledTools = session.EnabledTools,
            agentId = session.AgentId,
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SessionRecord body, CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        body.Id = id;
        body.UpdatedAt = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrEmpty(body.CreatedAt) || string.IsNullOrEmpty(body.Title))
        {
            var existing = await sessionStore.LoadAsync(id);
            if (string.IsNullOrEmpty(body.CreatedAt)) body.CreatedAt = existing?.CreatedAt ?? body.UpdatedAt;
            if (string.IsNullOrEmpty(body.Title)) body.Title = existing?.Title ?? "Chat";
        }

        await sessionStore.UpsertAsync(body);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (sessionStore is null) return NotFound(new { error = "Not found." });

        await sessionStore.DeleteAsync(id);
        return Ok(new { ok = true });
    }
}
