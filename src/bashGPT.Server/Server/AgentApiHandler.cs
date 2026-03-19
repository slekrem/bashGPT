using System.Net;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers;
using BashGPT.Agents;
using BashGPT.Configuration;

namespace BashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry, ConfigurationService? configService)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        var method = ctx.Request.HttpMethod;

        if (registry is null)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Agent registry is unavailable." }, statusCode: 503);
            return;
        }

        if (method == "GET" && path == "/api/agents")
        {
            await HandleListAsync(ctx.Response, ct);
            return;
        }

        if (method == "GET"
            && path.StartsWith("/api/agents/", StringComparison.Ordinal)
            && path.EndsWith("/info-panel", StringComparison.Ordinal))
        {
            var id = path["/api/agents/".Length..^"/info-panel".Length];
            await HandleInfoPanelAsync(ctx.Response, id, ct);
            return;
        }

        await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Not found." }, statusCode: 404);
    }

    private async Task HandleListAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var agents = registry!.All.Select(agent => new { id = agent.Id, name = agent.Name });
        await ApiResponse.WriteJsonAsync(response, new { agents });
    }

    private async Task HandleInfoPanelAsync(HttpListenerResponse response, string id, CancellationToken ct)
    {
        var agent = registry!.Find(id);
        if (agent is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { error = "Agent not found." }, statusCode: 404);
            return;
        }

        AgentLlmConfig? effectiveConfig = null;
        if (configService is not null)
        {
            var appConfig = await configService.LoadAsync();
            effectiveConfig = BuildEffectiveConfig(agent.LlmConfig, appConfig);
        }

        await ApiResponse.WriteJsonAsync(response, new { markdown = agent.GetInfoPanelMarkdown(effectiveConfig) });
    }

    private static AgentLlmConfig BuildEffectiveConfig(AgentLlmConfig? agentConfig, AppConfig appConfig)
    {
        var defaultModel = appConfig.Ollama.Model;
        var effectiveModel = agentConfig?.Model ?? defaultModel;

        return agentConfig is not null
            ? agentConfig with { Model = effectiveModel }
            : new AgentLlmConfig(Model: effectiveModel);
    }
}
