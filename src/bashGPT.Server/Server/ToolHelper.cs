using bashGPT.Core.Models.Providers;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

/// <summary>
/// Helper methods for resolving tool names to LLM tool definitions.
/// </summary>
internal static class ToolHelper
{
    /// <summary>
    /// Resolves a list of tool names against the tool registry and returns
    /// the matching LLM tool definitions.
    /// </summary>
    public static IReadOnlyList<ToolDefinition> Resolve(
        IEnumerable<string>? toolNames,
        ToolRegistry? registry)
    {
        if (toolNames is null || registry is null)
            return [];

        var result = new List<ToolDefinition>();
        foreach (var name in toolNames)
        {
            if (registry.TryGet(name, out var tool) && tool is not null)
                result.Add(ToLlmDefinition(tool.Definition));
        }
        return result;
    }

    private static ToolDefinition ToLlmDefinition(Tools.Abstractions.ToolDefinition def)
    {
        var required = def.Parameters
            .Where(p => p.Required)
            .Select(p => p.Name)
            .ToArray();

        var properties = def.Parameters.ToDictionary(
            p => p.Name,
            p => p.Type == "object"
                ? (object)new { type = p.Type, description = p.Description, additionalProperties = new { type = "string" } }
                : (object)new { type = p.Type, description = p.Description });

        return new ToolDefinition(
            Name: def.Name,
            Description: def.Description,
            Parameters: new { type = "object", properties, required });
    }
}
