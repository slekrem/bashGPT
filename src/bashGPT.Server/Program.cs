using System.CommandLine;
using BashGPT;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Tools.Execution;
using BashGPT.Tools.Fetch;
using BashGPT.Tools.Filesystem;
using BashGPT.Tools.Build;
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
]);
var serverRunner = new ServerChatRunner(configService, toolRegistry: toolRegistry);
var sessionStore = AppBootstrap.CreateSessionStore();
var agentRegistry = new AgentRegistry([new GenericAgent(), new DevAgent(), new ShellAgent()]);
var serverHost = new ServerHost(serverRunner, configService, sessionStore, agentRegistry, toolRegistry);

var providerOpt = new Option<string?>("--provider", "-p")
{
    Description = "LLM-Provider: 'ollama' oder 'cerebras' (überschreibt Config)"
};
var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Modellname (überschreibt Config)"
};
var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Debug-Ausgaben anzeigen"
};
var portOpt = new Option<int>("--port")
{
    Description = "Port für den Server-Modus",
    DefaultValueFactory = _ => 5050
};
var noBrowserOpt = new Option<bool>("--no-browser")
{
    Description = "Browser beim Start nicht automatisch öffnen"
};

var rootCommand = new RootCommand("bashGPT Server");
rootCommand.Options.Add(providerOpt);
rootCommand.Options.Add(modelOpt);
rootCommand.Options.Add(verboseOpt);
rootCommand.Options.Add(portOpt);
rootCommand.Options.Add(noBrowserOpt);

rootCommand.SetAction(async (parseResult, ct) =>
{
    var providerOverride = AppBootstrap.ParseProviderOrThrow(parseResult.GetValue(providerOpt));

    var serverOptions = new ServerOptions(
        Port: parseResult.GetValue(portOpt),
        NoBrowser: parseResult.GetValue(noBrowserOpt),
        Provider: providerOverride,
        Model: parseResult.GetValue(modelOpt),
        Verbose: parseResult.GetValue(verboseOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await serverHost.RunAsync(serverOptions, cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();
