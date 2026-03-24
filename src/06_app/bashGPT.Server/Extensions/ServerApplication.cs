using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Agents;
using bashGPT.Plugins;
using bashGPT.Server.Agents;
using bashGPT.Tools.Registration;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Server.Extensions;

internal static class ServerApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static ToolRegistry CreateToolRegistry(IEnumerable<ITool>? pluginTools = null, ILogger? logger = null)
    {
        var registry = new ToolRegistry([]);

        if (pluginTools is null)
            return registry;

        foreach (var tool in pluginTools)
        {
            try
            {
                registry.Register(tool);
            }
            catch (InvalidOperationException)
            {
                logger?.LogWarning("[plugin] Tool '{ToolName}' is a duplicate and was skipped.", tool.Definition.Name);
            }
        }

        return registry;
    }

    public static AgentRegistry CreateAgentRegistry(IEnumerable<AgentBase>? pluginAgents = null, ILogger? logger = null)
    {
        var builtins = new AgentBase[] { new GenericAgent() };

        if (pluginAgents is null)
            return new AgentRegistry(builtins);

        var seenIds = new HashSet<string>(builtins.Select(a => a.Id), StringComparer.OrdinalIgnoreCase);
        var additionalAgents = new List<AgentBase>();

        foreach (var agent in pluginAgents)
        {
            if (!seenIds.Add(agent.Id))
            {
                logger?.LogWarning("[plugin] Agent '{AgentId}' conflicts with an existing agent and was skipped.", agent.Id);
                continue;
            }

            additionalAgents.Add(agent);
        }

        return new AgentRegistry(builtins.Concat(additionalAgents));
    }

    /// <summary>
    /// Scans the user config plugins directory, loads all plugins, and reports
    /// non-fatal loading errors via the provided logger (or silently if none given).
    /// </summary>
    public static PluginLoadResult LoadPlugins(string? userPluginDir = null, ILogger? logger = null)
    {
        var dirs = new[]
        {
            userPluginDir ?? AppBootstrap.GetPluginsDir(),
        };

        var result = PluginLoader.LoadFromDirectories(dirs);

        foreach (var error in result.Errors)
            logger?.LogWarning("[plugin] {File}: {Message}", Path.GetFileName(error.Source), error.Message);

        return result;
    }

}
