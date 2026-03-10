using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Agents;

namespace BashGPT.Server;

internal sealed class AgentApiHandler(AgentStore? agentStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path   = ctx.Request.Url?.AbsolutePath ?? "/";
        var method = ctx.Request.HttpMethod;

        if (agentStore is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "AgentStore nicht verfügbar." }, statusCode: 503);
            return;
        }

        if (method == "GET" && path == "/api/agents")
        { await HandleListAsync(ctx.Response, ct); return; }

        if (method == "POST" && path == "/api/agents")
        { await HandleCreateAsync(ctx, ct); return; }

        if (method == "PATCH" && path.StartsWith("/api/agents/", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..];
            await HandlePatchAsync(ctx, id, ct);
            return;
        }

        if (method == "DELETE" && path.StartsWith("/api/agents/", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..];
            await HandleDeleteAsync(ctx.Response, id, ct);
            return;
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
    }

    private async Task HandleListAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var agents = await agentStore!.LoadAllAsync();
        await ApiResponse.WriteJsonAsync(response, new { agents = agents.Select(ToDto) });
    }

    private async Task HandleCreateAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        CreateAgentRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateAgentRequest>(
                ctx.Request.InputStream, JsonOptions, ct);
        }
        catch
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Ungültiger Request-Body." }, statusCode: 400);
            return;
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "name ist erforderlich." }, statusCode: 400);
            return;
        }

        var agent = new AgentRecord
        {
            Id           = Guid.NewGuid().ToString("N")[..8],
            Name         = body.Name.Trim(),
            SystemPrompt = body.SystemPrompt?.Trim(),
            EnabledTools = body.EnabledTools ?? [],
        };

        await agentStore!.UpsertAsync(agent);
        ctx.Response.StatusCode = 201;
        await ApiResponse.WriteJsonAsync(ctx.Response, ToDto(agent));
    }

    private async Task HandlePatchAsync(HttpListenerContext ctx, string id, CancellationToken ct)
    {
        var agent = await agentStore!.LoadAsync(id);
        if (agent is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Agent nicht gefunden." }, statusCode: 404);
            return;
        }

        PatchAgentRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<PatchAgentRequest>(
                ctx.Request.InputStream, JsonOptions, ct);
        }
        catch
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Ungültiger Request-Body." }, statusCode: 400);
            return;
        }

        if (body?.Name is not null)
        {
            var trimmed = body.Name.Trim();
            if (trimmed.Length == 0)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "name darf nicht leer sein." }, statusCode: 400);
                return;
            }
            agent.Name = trimmed;
        }
        if (body?.SystemPrompt is not null)
            agent.SystemPrompt = body.SystemPrompt.Trim().Length > 0 ? body.SystemPrompt.Trim() : null;
        if (body?.EnabledTools is not null)
            agent.EnabledTools = body.EnabledTools;

        await agentStore!.UpsertAsync(agent);
        await ApiResponse.WriteJsonAsync(ctx.Response, ToDto(agent));
    }

    private async Task HandleDeleteAsync(HttpListenerResponse response, string id, CancellationToken ct)
    {
        await agentStore!.DeleteAsync(id);
        await ApiResponse.WriteJsonAsync(response, new { ok = true });
    }

    private static object ToDto(AgentRecord a) => new
    {
        id           = a.Id,
        name         = a.Name,
        systemPrompt = a.SystemPrompt,
        enabledTools = a.EnabledTools,
    };

    private sealed record CreateAgentRequest(
        string? Name,
        string? SystemPrompt,
        List<string>? EnabledTools = null);

    private sealed record PatchAgentRequest(
        string? Name,
        string? SystemPrompt,
        List<string>? EnabledTools = null);
}
