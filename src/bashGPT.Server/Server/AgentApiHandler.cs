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
        Converters = { new JsonStringEnumConverter() },
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

        // GET /api/agents
        if (method == "GET" && path == "/api/agents")
        { await HandleListAsync(ctx.Response, ct); return; }

        // POST /api/agents
        if (method == "POST" && path == "/api/agents")
        { await HandleCreateAsync(ctx, ct); return; }

        // PATCH /api/agents/:id
        if (method == "PATCH" && path.StartsWith("/api/agents/", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..];
            await HandlePatchAsync(ctx, id, ct);
            return;
        }

        // DELETE /api/agents/:id
        if (method == "DELETE" && path.StartsWith("/api/agents/", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..];
            await HandleDeleteAsync(ctx.Response, id, ct);
            return;
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
    }

    // ── GET /api/agents ──────────────────────────────────────────────────────

    private async Task HandleListAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var agents = await agentStore!.LoadAllAsync();
        await ApiResponse.WriteJsonAsync(response, new { agents = agents.Select(ToDto) });
    }

    // ── POST /api/agents ─────────────────────────────────────────────────────

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

        var type = body.Type?.ToLowerInvariant() switch
        {
            "git" or "gitstatus"   => AgentCheckType.GitStatus,
            "http" or "httpstatus" => AgentCheckType.HttpStatus,
            "llm" or "llmagent"    => AgentCheckType.LlmAgent,
            _ => (AgentCheckType?)null
        };

        if (type is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "type muss 'git', 'http' oder 'llm' sein." }, statusCode: 400);
            return;
        }

        if (type == AgentCheckType.GitStatus && string.IsNullOrWhiteSpace(body.Path))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "path ist für Git-Agenten erforderlich." }, statusCode: 400);
            return;
        }

        if (type == AgentCheckType.HttpStatus && string.IsNullOrWhiteSpace(body.Url))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "url ist für HTTP-Agenten erforderlich." }, statusCode: 400);
            return;
        }

        if (type == AgentCheckType.LlmAgent && string.IsNullOrWhiteSpace(body.LoopInstruction))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "loopInstruction ist für LLM-Agenten erforderlich." }, statusCode: 400);
            return;
        }

        var agent = new AgentRecord
        {
            Id              = Guid.NewGuid().ToString("N")[..8],
            Name            = body.Name.Trim(),
            Type            = type.Value,
            Path            = body.Path?.Trim(),
            Url             = body.Url?.Trim(),
            IntervalSeconds = body.IntervalSeconds is > 0 ? body.IntervalSeconds.Value : 60,
            SystemPrompt    = body.SystemPrompt?.Trim(),
            LoopInstruction = body.LoopInstruction?.Trim(),
            ExecMode        = body.ExecMode?.Trim(),
            IsActive        = true,
        };

        await agentStore!.UpsertAsync(agent);
        ctx.Response.StatusCode = 201;
        await ApiResponse.WriteJsonAsync(ctx.Response, ToDto(agent));
    }

    // ── PATCH /api/agents/:id ────────────────────────────────────────────────

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

        if (body?.IsActive is bool active)
            agent.IsActive = active;
        if (body?.Name is not null)
        {
            var trimmedName = body.Name.Trim();
            if (trimmedName.Length == 0)
            {
                await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "name darf nicht leer sein." }, statusCode: 400);
                return;
            }
            agent.Name = trimmedName;
        }
        if (body?.IntervalSeconds is int interval && interval > 0)
            agent.IntervalSeconds = interval;
        if (body?.SystemPrompt is not null)
            agent.SystemPrompt = body.SystemPrompt.Trim().Length > 0 ? body.SystemPrompt.Trim() : null;
        if (body?.LoopInstruction is { Length: > 0 } loop)
            agent.LoopInstruction = loop.Trim();
        if (body?.ExecMode is not null)
            agent.ExecMode = body.ExecMode.Trim();

        await agentStore!.UpsertAsync(agent);
        await ApiResponse.WriteJsonAsync(ctx.Response, ToDto(agent));
    }

    // ── DELETE /api/agents/:id ───────────────────────────────────────────────

    private async Task HandleDeleteAsync(HttpListenerResponse response, string id, CancellationToken ct)
    {
        await agentStore!.DeleteAsync(id);
        await ApiResponse.WriteJsonAsync(response, new { ok = true });
    }

    // ── DTO ──────────────────────────────────────────────────────────────────

    private static object ToDto(AgentRecord a) => new
    {
        id                 = a.Id,
        name               = a.Name,
        type               = a.Type.ToString().ToLower(),
        path               = a.Path,
        url                = a.Url,
        intervalSeconds    = a.IntervalSeconds,
        systemPrompt       = a.SystemPrompt,
        loopInstruction    = a.LoopInstruction,
        execMode           = a.ExecMode,
        isActive           = a.IsActive,
        lastRun            = a.LastRun,
        lastMessage        = a.LastMessage,
        lastCheckSucceeded = a.LastCheckSucceeded,
    };

    // ── Request-DTOs ─────────────────────────────────────────────────────────

    private sealed record CreateAgentRequest(
        string? Name,
        string? Type,
        string? Path,
        string? Url,
        int? IntervalSeconds,
        string? SystemPrompt,
        string? LoopInstruction,
        string? ExecMode);

    private sealed record PatchAgentRequest(
        bool?   IsActive,
        string? Name,
        int?    IntervalSeconds,
        string? SystemPrompt,
        string? LoopInstruction,
        string? ExecMode);
}
