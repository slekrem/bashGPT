using bashGPT.Agents;
using bashGPT.Core.Configuration;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(AgentRegistry? registry, ConfigurationService? configService, ServerSessionService sessionService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        if (registry is null)
            return StatusCode(503, new { error = "Agent registry is unavailable." });

        var agents = registry.All.Select(a => new { id = a.Id, name = a.Name });
        return Ok(new { agents });
    }

    [HttpGet("{id}/info-panel")]
    public async Task<IActionResult> GetInfoPanel(string id, [FromQuery] string? sessionId, CancellationToken ct)
    {
        if (registry is null)
            return StatusCode(503, new { error = "Agent registry is unavailable." });

        var agent = registry.Find(id);
        if (agent is null)
            return NotFound(new { error = "Agent not found." });

        AgentLlmConfig? effectiveConfig = null;
        if (configService is not null)
        {
            var appConfig = await configService.LoadAsync();
            effectiveConfig = BuildEffectiveConfig(agent.LlmConfig, appConfig);
        }

        var sessionPath = sessionService.GetSessionPath(sessionId);
        return Ok(new { markdown = agent.GetInfoPanelMarkdown(effectiveConfig, sessionPath) });
    }

    private static AgentLlmConfig BuildEffectiveConfig(AgentLlmConfig? agentConfig, AppConfig appConfig)
    {
        var effectiveModel = agentConfig?.Model ?? appConfig.Ollama.Model;
        return agentConfig is not null
            ? agentConfig with { Model = effectiveModel }
            : new AgentLlmConfig(Model: effectiveModel);
    }
}
