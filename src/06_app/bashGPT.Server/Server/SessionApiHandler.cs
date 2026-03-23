using bashGPT.Core;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class SessionApiHandler(SessionStore? sessionStore)
{
    public async Task<IResult> GetAllAsync(CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        var sessions = await sessionStore.LoadAllAsync();
        return Results.Json(new
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

    public async Task<IResult> CreateAsync(CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        var now = DateTime.UtcNow.ToString("o");
        var newSession = new SessionRecord
        {
            Id = $"{AppDefaults.SessionIdPrefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Title = "New Chat",
            CreatedAt = now,
            UpdatedAt = now,
        };
        await sessionStore.UpsertAsync(newSession);
        return Results.Json(new
        {
            id = newSession.Id,
            title = newSession.Title,
            createdAt = newSession.CreatedAt,
            updatedAt = newSession.UpdatedAt,
        });
    }

    public async Task<IResult> ClearAsync(CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        await sessionStore.ClearAsync();
        return Results.Json(new { ok = true });
    }

    public async Task<IResult> GetByIdAsync(string id, CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        var session = await sessionStore.LoadAsync(id);
        if (session is null)
            return Results.Json(new { error = "Session not found." }, statusCode: 404);

        var visibleMessages = session.Messages
            .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrEmpty(m.Content))
            .ToList();

        return Results.Json(new
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

    public async Task<IResult> PutAsync(string id, HttpRequest req, CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        var body = await req.ReadFromJsonAsync<SessionRecord>(ct);
        if (body is null)
            return Results.Json(new { error = "Invalid request body." }, statusCode: 400);

        body.Id = id;
        body.UpdatedAt = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrEmpty(body.CreatedAt) || string.IsNullOrEmpty(body.Title))
        {
            var existing = await sessionStore.LoadAsync(id);
            if (string.IsNullOrEmpty(body.CreatedAt)) body.CreatedAt = existing?.CreatedAt ?? body.UpdatedAt;
            if (string.IsNullOrEmpty(body.Title)) body.Title = existing?.Title ?? "Chat";
        }

        await sessionStore.UpsertAsync(body);
        return Results.Json(new { ok = true });
    }

    public async Task<IResult> DeleteAsync(string id, CancellationToken ct)
    {
        if (sessionStore is null)
            return Results.Json(new { error = "Not found." }, statusCode: 404);

        await sessionStore.DeleteAsync(id);
        return Results.Json(new { ok = true });
    }
}
