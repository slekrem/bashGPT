using bashGPT.Core;
using bashGPT.Core.Configuration;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;
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
            registry.Register(tool);

        return registry;
    }

    public static AgentRegistry CreateAgentRegistry() =>
        new([new GenericAgent(), new DevAgent(), new ShellAgent()]);

    public static ServerHost CreateServerHost(
        ConfigurationService configService,
        ToolRegistry toolRegistry)
    {
        var sessionStore = AppBootstrap.CreateSessionStore();
        var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();
        var agentRegistry = CreateAgentRegistry();
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
