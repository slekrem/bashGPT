using bashGPT.Core;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class SessionApiHandler(SessionStore? sessionStore)
{
    public async Task GetAllAsync(HttpResponse response, CancellationToken ct)
    {
        if (sessionStore is null) { await response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        var sessions = await sessionStore.LoadAllAsync();
        await response.WriteJsonAsync(new
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

    public async Task CreateAsync(HttpResponse response, CancellationToken ct)
    {
        if (sessionStore is null) { await response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        var now = DateTime.UtcNow.ToString("o");
        var newSession = new SessionRecord
        {
            Id = $"{AppDefaults.SessionIdPrefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Title = "New Chat",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await sessionStore.UpsertAsync(newSession);
        await response.WriteJsonAsync(new
        {
            id = newSession.Id,
            title = newSession.Title,
            createdAt = newSession.CreatedAt,
            updatedAt = newSession.UpdatedAt,
        });
    }

    public async Task ClearAsync(HttpResponse response, CancellationToken ct)
    {
        if (sessionStore is null) { await response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        await sessionStore.ClearAsync();
        await response.WriteJsonAsync(new { ok = true });
    }

    public async Task GetByIdAsync(string id, HttpResponse response, CancellationToken ct)
    {
        if (sessionStore is null) { await response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        var session = await sessionStore.LoadAsync(id);
        if (session is null)
        {
            await response.WriteJsonAsync(new { error = "Session not found." }, statusCode: 404);
            return;
        }

        var visibleMessages = session.Messages
            .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrEmpty(m.Content))
            .ToList();

        await response.WriteJsonAsync(new
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

    public async Task PutAsync(string id, HttpContext ctx, CancellationToken ct)
    {
        if (sessionStore is null) { await ctx.Response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        var body = await ctx.Request.ReadFromJsonAsync<SessionRecord>(JsonDefaults.Options, ct);
        if (body is null)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Invalid request body." }, statusCode: 400);
            return;
        }

        body.Id = id;
        body.UpdatedAt = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrEmpty(body.CreatedAt) || string.IsNullOrEmpty(body.Title))
        {
            var existing = await sessionStore.LoadAsync(id);
            if (string.IsNullOrEmpty(body.CreatedAt)) body.CreatedAt = existing?.CreatedAt ?? body.UpdatedAt;
            if (string.IsNullOrEmpty(body.Title)) body.Title = existing?.Title ?? "Chat";
        }

        await sessionStore.UpsertAsync(body);
        await ctx.Response.WriteJsonAsync(new { ok = true });
    }

    public async Task DeleteAsync(string id, HttpResponse response, CancellationToken ct)
    {
        if (sessionStore is null) { await response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404); return; }

        await sessionStore.DeleteAsync(id);
        await response.WriteJsonAsync(new { ok = true });
    }
}
