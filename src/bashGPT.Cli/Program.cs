using System.CommandLine;
using bashGPT.Core;
using bashGPT.Core.Versioning;
using BashGPT.Cli;
using bashGPT.Core.Configuration;

if (args is ["--version"])
{
    var info = AppVersion.ForAssembly(typeof(Program).Assembly);
    Console.WriteLine($"{info.Application} {info.InformationalVersion}");
    if (!string.IsNullOrWhiteSpace(info.RepositoryUrl))
        Console.WriteLine(info.RepositoryUrl);
    return 0;
}

var configService = new ConfigurationService();
var cliRunner = new CliChatRunner(configService);

var modelOpt = new Option<string?>("--model", "-m")
{
    Description = "Modellname (überschreibt Config)"
};

var verboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Debug-Ausgaben anzeigen"
};

var forceToolsOpt = new Option<bool?>("--force-tools")
{
    Description = "Tool-Calls erzwingen (tool_choice=bash)"
};

var promptArg = new Argument<string[]>("prompt")
{
    Description = "Die Anfrage an das LLM",
    Arity = ArgumentArity.ZeroOrMore
};

var rootCommand = new RootCommand("bashGPT - KI-gestützter Shell-Assistent");

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
        Console.Error.WriteLine("Bitte gib eine Anfrage an.");
        Console.Error.WriteLine("Beispiel: bashgpt \"zeige alle .cs Dateien\"");
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

var configCommand = new Command("config", "Konfiguration lesen und setzen");

var configListCommand = new Command("list", "Alle Einstellungen anzeigen");
configListCommand.SetAction(async (_, _) =>
    Console.WriteLine(await configService.ListAsync()));

var configGetKey = new Argument<string>("key") { Description = "Konfigurationsschlüssel" };
var configGetCommand = new Command("get", "Einen Wert lesen");
configGetCommand.Arguments.Add(configGetKey);
configGetCommand.SetAction(async (parseResult, _) =>
{
    try { Console.WriteLine(await configService.GetAsync(parseResult.GetValue(configGetKey)!)); }
    catch (ArgumentException ex) { Console.Error.WriteLine($"Fehler: {ex.Message}"); }
});

var configSetKey = new Argument<string>("key") { Description = "Konfigurationsschlüssel" };
var configSetValue = new Argument<string>("value") { Description = "Neuer Wert" };
var configSetCommand = new Command("set", "Einen Wert setzen");
configSetCommand.Arguments.Add(configSetKey);
configSetCommand.Arguments.Add(configSetValue);
configSetCommand.SetAction(async (parseResult, _) =>
{
    var key = parseResult.GetValue(configSetKey)!;
    var value = parseResult.GetValue(configSetValue)!;
    try
    {
        await configService.SetAsync(key, value);
        Console.WriteLine($"✓ {key} = {value}");
    }
    catch (ArgumentException ex) { Console.Error.WriteLine($"Fehler: {ex.Message}"); }
});

configCommand.Subcommands.Add(configListCommand);
configCommand.Subcommands.Add(configGetCommand);
configCommand.Subcommands.Add(configSetCommand);
rootCommand.Subcommands.Add(configCommand);

return await rootCommand.Parse(args).InvokeAsync();
