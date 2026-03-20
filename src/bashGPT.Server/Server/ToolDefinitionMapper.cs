using bashGPT.Core.Models.Providers;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

/// <summary>
/// Maps registered runtime tools to provider-side LLM tool definitions.
/// </summary>
internal static class ToolDefinitionMapper
{
    /// <summary>
    /// Resolves a list of tool names against the runtime registry and returns
    /// the provider-facing tool definitions.
    /// </summary>
    public static IReadOnlyList<ToolDefinition> ResolveDefinitions(
        IEnumerable<string>? toolNames,
        ToolRegistry? registry)
    {
        if (toolNames is null || registry is null)
            return [];

        var result = new List<ToolDefinition>();
        foreach (var name in toolNames)
        {
            if (registry.TryGet(name, out var tool) && tool is not null)
                result.Add(ToProviderDefinition(tool.Definition));
        }

        return result;
    }

    // Tool contracts stay in bashGPT.Tools. The provider-facing schema belongs
    // to the server/provider boundary, so the conversion lives here.
    private static ToolDefinition ToProviderDefinition(bashGPT.Tools.Abstractions.ToolDefinition definition)
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

        return new ToolDefinition(
            Name: definition.Name,
            Description: definition.Description,
            Parameters: new { type = "object", properties, required });
    }
}
