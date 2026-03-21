using bashGPT.Agents;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Plugins;

/// <summary>
/// Holds the outcome of a plugin directory scan:
/// all tools and agents that were successfully discovered,
/// plus a list of non-fatal errors that were collected during loading.
/// </summary>
public sealed record PluginLoadResult(
    IReadOnlyList<ITool> Tools,
    IReadOnlyList<AgentBase> Agents,
    IReadOnlyList<PluginLoadError> Errors)
{
    /// <summary>An empty result with no tools, agents, or errors.</summary>
    public static readonly PluginLoadResult Empty = new([], [], []);
}
