using System.Text.Json;
using bashGPT.Core;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;

namespace bashGPT.Server;

internal sealed class SessionApiHandler(SessionStore? sessionStore)
{
    public async Task HandleAsync(HttpContext ctx, CancellationToken ct)
    {
        if (sessionStore is null)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404);
            return;
        }

        var req = ctx.Request;
        var path = req.Path.Value ?? "/";

        if (req.Method == "GET" && path == "/api/sessions")
        {
            var sessions = await sessionStore.LoadAllAsync();
            await ctx.Response.WriteJsonAsync(new
            {
                sessions = sessions.Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    createdAt = s.CreatedAt,
                    updatedAt = s.UpdatedAt,
                })
            });
            return;
        }

        if (req.Method == "POST" && path == "/api/sessions")
        {
            var now = DateTime.UtcNow.ToString("o");
            var newSession = new SessionRecord
            {
                Id = $"{AppDefaults.SessionIdPrefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Title = "New Chat",
                CreatedAt = now,
                UpdatedAt = now,
            };
            await sessionStore.UpsertAsync(newSession);
            await ctx.Response.WriteJsonAsync(new
            {
                id = newSession.Id,
                title = newSession.Title,
                createdAt = newSession.CreatedAt,
                updatedAt = newSession.UpdatedAt,
            });
            return;
        }

        if (req.Method == "POST" && path == "/api/sessions/clear")
        {
            await sessionStore.ClearAsync();
            await ctx.Response.WriteJsonAsync(new { ok = true });
            return;
        }

        if (req.Method == "GET" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            var session = await sessionStore.LoadAsync(id);
            if (session is null)
            {
                await ctx.Response.WriteJsonAsync(new { error = "Session not found." }, statusCode: 404);
                return;
            }

            var visibleMessages = session.Messages
                .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrEmpty(m.Content))
                .ToList();

            await ctx.Response.WriteJsonAsync(new
            {
                id = session.Id,
                title = session.Title,
                createdAt = session.CreatedAt,
                updatedAt = session.UpdatedAt,
                messages = visibleMessages,
                enabledTools = session.EnabledTools,
                agentId = session.AgentId,
            });
            return;
        }

        if (req.Method == "PUT" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            var body = await req.ReadFromJsonAsync<SessionRecord>(JsonDefaults.Options, ct);
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
            return;
        }

        if (req.Method == "DELETE" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            await sessionStore.DeleteAsync(id);
            await ctx.Response.WriteJsonAsync(new { ok = true });
            return;
        }

        await ctx.Response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404);
    }
}
