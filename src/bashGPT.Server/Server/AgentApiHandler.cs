using System.Net;
using BashGPT.Agents;
using BashGPT.Configuration;

namespace BashGPT.Server;

internal sealed class AgentApiHandler(AgentRegistry? registry, ConfigurationService? configService)
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
        if (appConfig.DefaultProvider == ProviderType.Ollama)
        {
            var p = appConfig.Ollama;
            return new AgentLlmConfig(
                Model:             agentConfig?.Model ?? p.Model,
                Temperature:       agentConfig?.Temperature ?? p.Temperature,
                TopP:              agentConfig?.TopP ?? p.TopP,
                NumCtx:            agentConfig?.NumCtx ?? p.NumCtx,
                MaxTokens:         agentConfig?.MaxTokens,
                Seed:              agentConfig?.Seed ?? p.Seed,
                ReasoningEffort:   agentConfig?.ReasoningEffort,
                ParallelToolCalls: agentConfig?.ParallelToolCalls,
                Stream:            agentConfig?.Stream ?? true
            );
        }
        else
        {
            var p = appConfig.Cerebras;
            return new AgentLlmConfig(
                Model:             agentConfig?.Model ?? p.Model,
                Temperature:       agentConfig?.Temperature ?? p.Temperature,
                TopP:              agentConfig?.TopP ?? p.TopP,
                NumCtx:            null,
                MaxTokens:         agentConfig?.MaxTokens ?? p.MaxCompletionTokens,
                Seed:              agentConfig?.Seed ?? p.Seed,
                ReasoningEffort:   agentConfig?.ReasoningEffort ?? p.ReasoningEffort,
                ParallelToolCalls: agentConfig?.ParallelToolCalls,
                Stream:            agentConfig?.Stream ?? true
            );
        }
    }
}
