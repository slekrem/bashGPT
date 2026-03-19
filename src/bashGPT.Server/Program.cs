using System.CommandLine;
using bashGPT.Core;
using bashGPT.Core.Configuration;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;
using BashGPT.Server;
using BashGPT.Tools.Build;
using BashGPT.Tools.Execution;
using BashGPT.Tools.Fetch;
using BashGPT.Tools.Filesystem;
using BashGPT.Tools.Git;
using BashGPT.Tools.Shell;
using BashGPT.Tools.Testing;

var configService = new ConfigurationService();
var toolRegistry = new ToolRegistry([
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
var serverRunner = new ServerChatRunner(configService, toolRegistry: toolRegistry);
var sessionStore = AppBootstrap.CreateSessionStore();
var sessionRequestStore = AppBootstrap.CreateSessionRequestStore();
var agentRegistry = new AgentRegistry([new GenericAgent(), new DevAgent(), new ShellAgent()]);
var serverHost = new ServerHost(serverRunner, configService, sessionStore, sessionRequestStore, agentRegistry, toolRegistry);

var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Model name (overrides config)"
};
var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Show debug output"
};
var portOpt = new Option<int>("--port")
{
    Description = "Port for server mode",
    DefaultValueFactory = _ => 5050
};
var noBrowserOpt = new Option<bool>("--no-browser")
{
    Description = "Do not open the browser automatically on startup"
};

var rootCommand = new RootCommand("bashGPT Server");
rootCommand.Options.Add(modelOpt);
rootCommand.Options.Add(verboseOpt);
rootCommand.Options.Add(portOpt);
rootCommand.Options.Add(noBrowserOpt);

rootCommand.SetAction(async (parseResult, ct) =>
{
    var serverOptions = new ServerOptions(
        Port: parseResult.GetValue(portOpt),
        NoBrowser: parseResult.GetValue(noBrowserOpt),
        Model: parseResult.GetValue(modelOpt),
        Verbose: parseResult.GetValue(verboseOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await serverHost.RunAsync(serverOptions, cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();
