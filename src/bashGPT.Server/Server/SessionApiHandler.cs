using System.Net;
using System.Text.Json;
using bashGPT.Core;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Serialization;
using bashGPT.Core.Storage;

namespace BashGPT.Server;

internal sealed class SessionApiHandler(SessionStore? sessionStore)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        if (sessionStore is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Not found." }, statusCode: 404);
            return;
        }

        var req = ctx.Request;
        var path = req.Url?.AbsolutePath ?? "/";

        if (req.HttpMethod == "GET" && path == "/api/sessions")
        {
            var sessions = await sessionStore.LoadAllAsync();
            await ApiResponse.WriteJsonAsync(ctx.Response, new
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

        if (req.HttpMethod == "POST" && path == "/api/sessions")
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
            await ApiResponse.WriteJsonAsync(ctx.Response, new
            {
                id = newSession.Id,
                title = newSession.Title,
                createdAt = newSession.CreatedAt,
                updatedAt = newSession.UpdatedAt,
            });
            return;
        }

        if (req.HttpMethod == "POST" && path == "/api/sessions/clear")
        {
            await sessionStore.ClearAsync();
            await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
            return;
        }

        if (req.HttpMethod == "GET" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            var session = await sessionStore.LoadAsync(id);
            if (session is null)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Session not found." }, statusCode: 404);
                return;
            }

            var visibleMessages = session.Messages
                .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrEmpty(m.Content))
                .ToList();

            await ApiResponse.WriteJsonAsync(ctx.Response, new
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

        if (req.HttpMethod == "PUT" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            var body = await JsonSerializer.DeserializeAsync<SessionRecord>(req.InputStream, JsonDefaults.Options, ct);
            if (body is null)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Invalid request body." }, statusCode: 400);
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
            await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
            return;
        }

        if (req.HttpMethod == "DELETE" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id = path["/api/sessions/".Length..];
            await sessionStore.DeleteAsync(id);
            await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
            return;
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Not found." }, statusCode: 404);
    }
}
