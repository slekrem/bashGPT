using System.CommandLine;
using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Core.Versioning;
using bashGPT.Cli;
using Serilog;

var logsDir = AppBootstrap.GetLogsDir();
Directory.CreateDirectory(logsDir);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File(
        Path.Combine(logsDir, "cli-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

if (args is ["--version"])
{
    var info = AppVersion.ForAssembly(typeof(Program).Assembly);
    Console.WriteLine($"{info.Application} {info.InformationalVersion}");
    if (!string.IsNullOrWhiteSpace(info.RepositoryUrl))
        Console.WriteLine(info.RepositoryUrl);
    return 0;
}

var configService = CliApplication.CreateConfigurationService();
var pluginResult = CliApplication.LoadPlugins();
var cliRunner = CliApplication.CreateChatRunner(configService, pluginResult.Tools);

var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Model name (overrides config)"
};

var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Show debug output"
};

var forceToolsOpt = new Option<bool?>("--force-tools")
{
    Description = "Force tool calls (tool_choice=bash)"
};

var promptArg = new Argument<string[]>("prompt")
{
    Description = "The prompt to send to the LLM",
    Arity = ArgumentArity.ZeroOrMore
};

var rootCommand = new RootCommand("bashGPT - AI-powered shell assistant");

rootCommand.Arguments.Add(promptArg);
rootCommand.Options.Add(modelOpt);
rootCommand.Options.Add(verboseOpt);
rootCommand.Options.Add(forceToolsOpt);

rootCommand.SetAction(async (parseResult, ct) =>
{
    var promptParts = parseResult.GetValue(promptArg) ?? [];
    var prompt = string.Join(" ", promptParts).Trim();

    if (string.IsNullOrWhiteSpace(prompt))
    {
        Console.Error.WriteLine("Please provide a prompt.");
        Console.Error.WriteLine("Example: bashgpt \"show all .cs files\"");
        return;
    }

    var opts = new CliOptions(
        Prompt: prompt,
        Model: parseResult.GetValue(modelOpt),
        NoContext: false,
        IncludeDir: false,
        Verbose: parseResult.GetValue(verboseOpt),
        ForceTools: parseResult.GetValue(forceToolsOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await cliRunner.RunAsync(opts, cts.Token);
});

var configCommand = new Command("config", "Read and update configuration");

var configListCommand = new Command("list", "Show all settings");
configListCommand.SetAction(async (_, _) =>
    Console.WriteLine(await configService.ListAsync()));

var configGetKey = new Argument<string>("key") { Description = "Configuration key" };
var configGetCommand = new Command("get", "Read a setting value");
configGetCommand.Arguments.Add(configGetKey);
configGetCommand.SetAction(async (parseResult, _) =>
{
    try { Console.WriteLine(await configService.GetAsync(parseResult.GetValue(configGetKey)!)); }
    catch (ArgumentException ex) { Console.Error.WriteLine($"Error: {ex.Message}"); }
});

var configSetKey = new Argument<string>("key") { Description = "Configuration key" };
var configSetValue = new Argument<string>("value") { Description = "New value" };
var configSetCommand = new Command("set", "Update a setting value");
configSetCommand.Arguments.Add(configSetKey);
configSetCommand.Arguments.Add(configSetValue);
configSetCommand.SetAction(async (parseResult, _) =>
{
    var key = parseResult.GetValue(configSetKey)!;
    var value = parseResult.GetValue(configSetValue)!;
    try
    {
        await configService.SetAsync(key, value);
        Console.WriteLine($"OK {key} = {value}");
    }
    catch (ArgumentException ex) { Console.Error.WriteLine($"Error: {ex.Message}"); }
});

configCommand.Subcommands.Add(configListCommand);
configCommand.Subcommands.Add(configGetCommand);
configCommand.Subcommands.Add(configSetCommand);
rootCommand.Subcommands.Add(configCommand);

try
{
    return await rootCommand.Parse(args).InvokeAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
