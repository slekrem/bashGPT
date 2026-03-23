using bashGPT.Core.Providers.Abstractions;
using bashGPT.Agents;
using bashGPT.Core.Configuration;

namespace bashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry, ConfigurationService? configService)
{
    public async Task HandleAsync(HttpContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Path.Value ?? "/";
        var method = ctx.Request.Method;

        if (registry is null)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Agent registry is unavailable." }, statusCode: 503);
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

        await ctx.Response.WriteJsonAsync(new { error = "Not found." }, statusCode: 404);
    }

    private async Task HandleListAsync(HttpResponse response, CancellationToken ct)
    {
        var agents = registry!.All.Select(agent => new { id = agent.Id, name = agent.Name });
        await response.WriteJsonAsync(new { agents });
    }

    private async Task HandleInfoPanelAsync(HttpResponse response, string id, CancellationToken ct)
    {
        var agent = registry!.Find(id);
        if (agent is null)
        {
            await response.WriteJsonAsync(new { error = "Agent not found." }, statusCode: 404);
            return;
        }

        AgentLlmConfig? effectiveConfig = null;
        if (configService is not null)
        {
            var appConfig = await configService.LoadAsync();
            effectiveConfig = BuildEffectiveConfig(agent.LlmConfig, appConfig);
        }

        await response.WriteJsonAsync(new { markdown = agent.GetInfoPanelMarkdown(effectiveConfig) });
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
