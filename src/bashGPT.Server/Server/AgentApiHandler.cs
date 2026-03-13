using System.Net;
using BashGPT.Agents;

namespace BashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path   = ctx.Request.Url?.AbsolutePath ?? "/";
        var method = ctx.Request.HttpMethod;

        if (registry is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "AgentRegistry nicht verfügbar." }, statusCode: 503);
            return;
        }

        if (method == "GET" && path == "/api/agents")
        { await HandleListAsync(ctx.Response, ct); return; }

        if (method == "GET" && path.StartsWith("/api/agents/", StringComparison.Ordinal) && path.EndsWith("/info-panel", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..^"/info-panel".Length];
            await HandleInfoPanelAsync(ctx.Response, id, ct);
            return;
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
    }

    private async Task HandleListAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var agents = registry!.All.Select(a => new { id = a.Id, name = a.Name });
        await ApiResponse.WriteJsonAsync(response, new { agents });
    }

    private async Task HandleInfoPanelAsync(HttpListenerResponse response, string id, CancellationToken ct)
    {
        var agent = registry!.Find(id);
        if (agent is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Agent nicht gefunden." }, statusCode: 404);
            return;
        }

        await ApiResponse.WriteJsonAsync(response, new { markdown = agent.GetInfoPanelMarkdown() });
    }
}
