using System.CommandLine;
using bashGPT.Core.Configuration;
using bashGPT.Server;

var configService = ServerApplication.CreateConfigurationService();
var pluginResult = ServerApplication.LoadPlugins();
var toolRegistry = ServerApplication.CreateToolRegistry(pluginResult.Tools);
var pluginToolNames = pluginResult.Tools.Select(t => t.Definition.Name);
var serverHost = ServerApplication.CreateServerHost(configService, toolRegistry, pluginResult.Agents, pluginToolNames);

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
