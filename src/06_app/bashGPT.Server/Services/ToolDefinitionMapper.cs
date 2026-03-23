using bashGPT.Agents;
using bashGPT.Core.Models.Providers;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

/// <summary>
/// Maps registered runtime tools to provider-side LLM tool definitions.
/// </summary>
internal static class ToolDefinitionMapper
{
    /// <summary>
    /// Resolves a list of tool names against the runtime registry and returns
    /// the provider-facing tool definitions. Agent-owned tools take precedence
    /// over the registry; only names not covered by the agent are looked up there.
    /// </summary>
    public static IReadOnlyList<ProviderToolDefinition> ResolveDefinitions(
        IEnumerable<string>? toolNames,
        ToolRegistry? registry,
        AgentBase? agent = null)
    {
        if (toolNames is null)
            return [];

        var ownedByAgent = BuildOwnedIndex(agent);

        var result = new List<ProviderToolDefinition>();
        foreach (var name in toolNames)
        {
            if (ownedByAgent.TryGetValue(name, out var ownedTool))
            {
                result.Add(ToProviderDefinition(ownedTool.Definition));
            }
            else if (registry is not null && registry.TryGet(name, out var registryTool) && registryTool is not null)
            {
                result.Add(ToProviderDefinition(registryTool.Definition));
            }
        }

        return result;
    }

    private static Dictionary<string, ITool> BuildOwnedIndex(AgentBase? agent)
    {
        if (agent is null) return [];
        var owned = agent.GetOwnedTools();
        return owned.Count == 0
            ? []
            : owned.ToDictionary(t => t.Definition.Name, StringComparer.Ordinal);
    }

    // Tool contracts stay in bashGPT.Tools. The provider-facing schema belongs
    // to the server/provider boundary, so the conversion lives here.
    private static ProviderToolDefinition ToProviderDefinition(bashGPT.Tools.Abstractions.ToolDefinition definition)
    {
        var required = definition.Parameters
            .Where(parameter => parameter.Required)
            .Select(parameter => parameter.Name)
            .ToArray();

        var properties = definition.Parameters.ToDictionary(
            parameter => parameter.Name,
            parameter => parameter.Type == "object"
                ? (object)new
                {
                    type = parameter.Type,
                    description = parameter.Description,
                    additionalProperties = new { type = "string" }
                }
                : new
                {
                    type = parameter.Type,
                    description = parameter.Description
                });

        return new ProviderToolDefinition(
            Name: definition.Name,
            Description: definition.Description,
            Parameters: new { type = "object", properties, required });
    }
}
