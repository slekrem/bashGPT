using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Agents;
using bashGPT.Agents.Dev;
using bashGPT.Agents.Shell;
using bashGPT.Plugins;
using bashGPT.Server.Agents;
using bashGPT.Tools.Build;
using bashGPT.Tools.Registration;
using bashGPT.Tools.Fetch;
using bashGPT.Tools.Filesystem;
using bashGPT.Tools.Git;
using bashGPT.Tools.Shell;
using bashGPT.Tools.Testing;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Server;

internal static class ServerApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static IReadOnlyList<ITool> CreateDefaultTools() =>
    [
            new ShellExecTool(),
            new FetchTool(),
            new FilesystemReadTool(),
            new FilesystemWriteTool(),
            new FilesystemSearchTool(),
            new GitStatusTool(),
            new GitDiffTool(),
            new GitLogTool(),
            new GitBranchTool(),
            new GitAddTool(),
            new GitCommitTool(),
            new GitCheckoutTool(),
            new TestRunTool(),
            new BuildRunTool(),
            new ContextLoadFilesTool(),
            new ContextUnloadFilesTool(),
            new ContextClearFilesTool(),
    ];

    public static ToolRegistry CreateToolRegistry(IEnumerable<ITool>? additionalTools = null)
    {
        var registry = new ToolRegistry(CreateDefaultTools());

        if (additionalTools is null)
            return registry;

        foreach (var tool in additionalTools)
        {
            try
            {
                registry.Register(tool);
            }
            catch (InvalidOperationException)
            {
                Console.Error.WriteLine(
                    $"[plugin] Tool '{tool.Definition.Name}' conflicts with a built-in tool and was skipped.");
            }
        }

        return registry;
    }

    public static AgentRegistry CreateAgentRegistry(IEnumerable<AgentBase>? additionalAgents = null)
    {
        var builtins = new AgentBase[] { new GenericAgent(), new DevAgent(), new ShellAgent() };

        if (additionalAgents is null)
            return new AgentRegistry(builtins);

        var seenIds = new HashSet<string>(builtins.Select(a => a.Id), StringComparer.OrdinalIgnoreCase);
        var pluginAgents = new List<AgentBase>();

        foreach (var agent in additionalAgents)
        {
            if (!seenIds.Add(agent.Id))
            {
                Console.Error.WriteLine(
                    $"[plugin] Agent '{agent.Id}' conflicts with an existing agent and was skipped.");
                continue;
            }

            pluginAgents.Add(agent);
        }

        return new AgentRegistry(builtins.Concat(pluginAgents));
    }

    /// <summary>
    /// Scans the plugin directory and returns all discovered tools and agents.
    /// Non-fatal loading errors are written to <see cref="Console.Error"/>.
    /// </summary>
    public static PluginLoadResult LoadPlugins(string? pluginDir = null)
    {
        var dir = pluginDir ?? AppBootstrap.GetPluginsDir();
        var result = PluginLoader.LoadFromDirectory(dir);

        foreach (var error in result.Errors)
            Console.Error.WriteLine($"[plugin] {Path.GetFileName(error.Source)}: {error.Message}");

        return result;
    }

    public static ServerHost CreateServerHost(
        ConfigurationService configService,
        ToolRegistry toolRegistry,
        IEnumerable<AgentBase>? additionalAgents = null)
    {
        var sessionStore = AppBootstrap.CreateSessionStore();
        var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();
        var agentRegistry = CreateAgentRegistry(additionalAgents);
        var serverRunner = new ServerChatRunner(configService, toolRegistry: toolRegistry);

        return new ServerHost(
            serverRunner,
            configService,
            sessionStore,
            sessionRequestStore,
            agentRegistry,
            toolRegistry);
    }
}
