using System.CommandLine;
using BashGPT;
using BashGPT.Agents;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Shell;

var configService = new ConfigurationService();
var contextCollector = new ShellContextCollector();
var serverRunner = new ServerChatRunner(configService, contextCollector);
var sessionStore = AppBootstrap.CreateSessionStore();
var agentStore = AgentBootstrap.CreateAgentStore();
var serverHost = new ServerHost(serverRunner, configService, sessionStore, agentStore);

var providerOpt = new Option<string?>("--provider", "-p")
{
    Description = "LLM-Provider: 'ollama' oder 'cerebras' (überschreibt Config)"
};
var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Modellname (überschreibt Config)"
};
var noContextOpt = new Option<bool>("--no-context")
{
    Description = "Kein Shell-Kontext mitschicken"
};
var includeDirOpt = new Option<bool>("--include-dir")
{
    Description = "Verzeichnisinhalt in den Kontext aufnehmen"
};
var autoExecOpt = new Option<bool>("--auto-exec", "-y")
{
    Description = "Befehle ohne Bestätigung ausführen"
};
var dryRunOpt = new Option<bool>("--dry-run")
{
    Description = "Befehle anzeigen, aber nie ausführen"
};
var noExecOpt = new Option<bool>("--no-exec")
{
    Description = "Keine Befehle ausführen (reiner Chat-Modus)"
};
var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Debug-Ausgaben anzeigen"
};
var forceToolsOpt = new Option<bool?>("--force-tools")
{
    Description = "Tool-Calls erzwingen (tool_choice=bash)"
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
rootCommand.Options.Add(noContextOpt);
rootCommand.Options.Add(includeDirOpt);
rootCommand.Options.Add(autoExecOpt);
rootCommand.Options.Add(dryRunOpt);
rootCommand.Options.Add(noExecOpt);
rootCommand.Options.Add(verboseOpt);
rootCommand.Options.Add(forceToolsOpt);
rootCommand.Options.Add(portOpt);
rootCommand.Options.Add(noBrowserOpt);

rootCommand.SetAction(async (parseResult, ct) =>
{
    var providerOverride = AppBootstrap.ParseProviderOrThrow(parseResult.GetValue(providerOpt));
    var execMode = AppBootstrap.ResolveExecutionMode(
        noExec: parseResult.GetValue(noExecOpt),
        dryRun: parseResult.GetValue(dryRunOpt),
        autoExec: parseResult.GetValue(autoExecOpt));

    var serverOptions = new ServerOptions(
        Port: parseResult.GetValue(portOpt),
        NoBrowser: parseResult.GetValue(noBrowserOpt),
        Provider: providerOverride,
        Model: parseResult.GetValue(modelOpt),
        NoContext: parseResult.GetValue(noContextOpt),
        IncludeDir: parseResult.GetValue(includeDirOpt),
        ExecMode: execMode,
        Verbose: parseResult.GetValue(verboseOpt),
        ForceTools: parseResult.GetValue(forceToolsOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await serverHost.RunAsync(serverOptions, cts.Token);
});

return await rootCommand.Parse(args).InvokeAsync();
