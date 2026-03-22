using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Agents;
using bashGPT.Plugins;
using bashGPT.Server.Agents;
using bashGPT.Tools.Registration;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Server;

internal static class ServerApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static ToolRegistry CreateToolRegistry(IEnumerable<ITool>? pluginTools = null)
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
                Console.Error.WriteLine(
                    $"[plugin] Tool '{tool.Definition.Name}' is a duplicate and was skipped.");
            }
        }

        return registry;
    }

    public static AgentRegistry CreateAgentRegistry(IEnumerable<AgentBase>? pluginAgents = null)
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
                Console.Error.WriteLine(
                    $"[plugin] Agent '{agent.Id}' conflicts with an existing agent and was skipped.");
                continue;
            }

            additionalAgents.Add(agent);
        }

        return new AgentRegistry(builtins.Concat(additionalAgents));
    }

    /// <summary>
    /// Scans the user config plugins directory, loads all plugins, and reports
    /// non-fatal loading errors to stderr.
    /// </summary>
    public static PluginLoadResult LoadPlugins(string? userPluginDir = null)
    {
        var dirs = new[]
        {
            userPluginDir ?? AppBootstrap.GetPluginsDir(),
        };

        var result = PluginLoader.LoadFromDirectories(dirs);

        foreach (var error in result.Errors)
            Console.Error.WriteLine($"[plugin] {Path.GetFileName(error.Source)}: {error.Message}");

        return result;
    }

    public static ServerHost CreateServerHost(
        ConfigurationService configService,
        ToolRegistry toolRegistry,
        IEnumerable<AgentBase>? pluginAgents = null,
        IEnumerable<string>? pluginToolNames = null)
    {
        var sessionStore = AppBootstrap.CreateSessionStore();
        var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();
        var agentRegistry = CreateAgentRegistry(pluginAgents);
        var serverRunner = new ServerChatRunner(configService, toolRegistry: toolRegistry);

        // Plugin tools are explicitly installed by the user and are therefore trusted.
        // They are added on top of the environment-configured allowed tools.
        var toolSelectionPolicy = ServerToolSelectionPolicy.FromEnvironment(pluginToolNames);

        return new ServerHost(
            serverRunner,
            configService,
            sessionStore,
            sessionRequestStore,
            agentRegistry,
            toolRegistry,
            toolSelectionPolicy: toolSelectionPolicy);
    }
}
