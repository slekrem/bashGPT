using bashGPT.Core.Providers.Abstractions;
using bashGPT.Agents;
using bashGPT.Core.Configuration;

namespace bashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry, ConfigurationService? configService)
{
    public IResult GetAll()
    {
        if (registry is null)
            return Results.Json(new { error = "Agent registry is unavailable." }, statusCode: 503);

        var agents = registry.All.Select(a => new { id = a.Id, name = a.Name });
        return Results.Json(new { agents });
    }

    public async Task<IResult> GetInfoPanel(string id, CancellationToken ct)
    {
        if (registry is null)
            return Results.Json(new { error = "Agent registry is unavailable." }, statusCode: 503);

        var agent = registry.Find(id);
        if (agent is null)
            return Results.Json(new { error = "Agent not found." }, statusCode: 404);

        AgentLlmConfig? effectiveConfig = null;
        if (configService is not null)
        {
            var appConfig = await configService.LoadAsync();
            effectiveConfig = BuildEffectiveConfig(agent.LlmConfig, appConfig);
        }

        return Results.Json(new { markdown = agent.GetInfoPanelMarkdown(effectiveConfig) });
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
