using System.Net;
using System.Text.Json;
using BashGPT.Providers;
using BashGPT.Storage;

namespace BashGPT.Server;

internal sealed class SessionApiHandler(SessionStore? sessionStore, LegacyHistory legacyHistory)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        if (sessionStore is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
            return;
        }

        var req  = ctx.Request;
        var path = req.Url?.AbsolutePath ?? "/";

        if (req.HttpMethod == "GET" && path == "/api/sessions")
        {
            var sessions = await sessionStore.LoadAllAsync();
            await ApiResponse.WriteJsonAsync(ctx.Response, new
            {
                sessions = sessions.Select(s => new
                {
                    id = s.Id, title = s.Title,
                    createdAt = s.CreatedAt, updatedAt = s.UpdatedAt,
                })
            });
            return;
        }

        if (req.HttpMethod == "POST" && path == "/api/sessions")
        {
            var now = DateTime.UtcNow.ToString("o");
            var newSession = new SessionRecord
            {
                Id        = $"s-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Title     = "Neuer Chat",
                CreatedAt = now,
                UpdatedAt = now,
            };
            await sessionStore.UpsertAsync(newSession);
            await ApiResponse.WriteJsonAsync(ctx.Response, new
            {
                id = newSession.Id, title = newSession.Title,
                createdAt = newSession.CreatedAt, updatedAt = newSession.UpdatedAt,
            });
            return;
        }

        if (req.HttpMethod == "POST" && path == "/api/sessions/clear")
        {
            await sessionStore.ClearAsync();
            legacyHistory.Clear();
            await legacyHistory.PersistAsync();
            await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
            return;
        }

        if (req.HttpMethod == "GET" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id      = path["/api/sessions/".Length..];
            var session = await sessionStore.LoadAsync(id);
            if (session is null)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Session nicht gefunden." }, statusCode: 404);
                return;
            }
            await ApiResponse.WriteJsonAsync(ctx.Response, session);
            return;
        }

        if (req.HttpMethod == "PUT" && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            var id   = path["/api/sessions/".Length..];
            var body = await JsonSerializer.DeserializeAsync<SessionRecord>(req.InputStream, JsonDefaults.Options, ct);
            if (body is null)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Ungültiger Body." }, statusCode: 400);
                return;
            }
            body.Id        = id;
            body.UpdatedAt = DateTime.UtcNow.ToString("o");
            if (string.IsNullOrEmpty(body.CreatedAt) || string.IsNullOrEmpty(body.Title))
            {
                var existing = await sessionStore.LoadAsync(id);
                if (string.IsNullOrEmpty(body.CreatedAt)) body.CreatedAt = existing?.CreatedAt ?? body.UpdatedAt;
                if (string.IsNullOrEmpty(body.Title))     body.Title     = existing?.Title     ?? "Chat";
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

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
    }
}
