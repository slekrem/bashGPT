using bashGPT.Core;
using bashGPT.Core.Configuration;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;
using BashGPT.Tools.Build;
using BashGPT.Tools.Execution;
using BashGPT.Tools.Fetch;
using BashGPT.Tools.Filesystem;
using BashGPT.Tools.Git;
using BashGPT.Tools.Shell;
using BashGPT.Tools.Testing;

namespace BashGPT.Server;

internal static class ServerApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static ToolRegistry CreateToolRegistry() =>
        new([
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
        ]);

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
