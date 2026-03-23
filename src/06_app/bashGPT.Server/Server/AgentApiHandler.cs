using bashGPT.Core.Providers.Abstractions;
using bashGPT.Agents;
using bashGPT.Core.Configuration;

namespace bashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry, ConfigurationService? configService)
{
    public async Task GetAllAsync(HttpResponse response, CancellationToken ct)
    {
        if (registry is null)
        {
            await response.WriteJsonAsync(new { error = "Agent registry is unavailable." }, statusCode: 503);
            return;
        }

        var agents = registry.All.Select(agent => new { id = agent.Id, name = agent.Name });
        await response.WriteJsonAsync(new { agents });
    }

    public async Task GetInfoPanelAsync(string id, HttpResponse response, CancellationToken ct)
    {
        if (registry is null)
        {
            await response.WriteJsonAsync(new { error = "Agent registry is unavailable." }, statusCode: 503);
            return;
        }

        var agent = registry.Find(id);
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
