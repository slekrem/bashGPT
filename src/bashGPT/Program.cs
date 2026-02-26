using System.CommandLine;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Shell;

var configService    = new ConfigurationService();
var contextCollector = new ShellContextCollector();
var handler          = new PromptHandler(configService, contextCollector);
var serverHost       = new ServerHost(handler);

// ── Optionen ─────────────────────────────────────────────────────────────────

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

var forceToolsOpt = new Option<bool>("--force-tools")
{
    Description = "Tool-Calls erzwingen (tool_choice=bash)"
};

// ── Prompt-Argument ───────────────────────────────────────────────────────────

var promptArg = new Argument<string[]>("prompt")
{
    Description = "Die Anfrage an das LLM",
    Arity       = ArgumentArity.ZeroOrMore
};

// ── Root-Command ──────────────────────────────────────────────────────────────

var rootCommand = new RootCommand("bashGPT – KI-gestützter Shell-Assistent");

rootCommand.Arguments.Add(promptArg);
rootCommand.Options.Add(providerOpt);
rootCommand.Options.Add(modelOpt);
rootCommand.Options.Add(noContextOpt);
rootCommand.Options.Add(includeDirOpt);
rootCommand.Options.Add(autoExecOpt);
rootCommand.Options.Add(dryRunOpt);
rootCommand.Options.Add(noExecOpt);
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

    var providerStr     = parseResult.GetValue(providerOpt);
    ProviderType? providerOverride = providerStr?.ToLowerInvariant() switch
    {
        "ollama"   => ProviderType.Ollama,
        "cerebras" => ProviderType.Cerebras,
        null       => null,
        var v      => throw new ArgumentException($"Unbekannter Provider '{v}'. Erlaubt: ollama, cerebras")
    };

    var autoExec = parseResult.GetValue(autoExecOpt);
    var dryRun   = parseResult.GetValue(dryRunOpt);
    var noExec   = parseResult.GetValue(noExecOpt);

    var execMode = (noExec, dryRun, autoExec) switch
    {
        (true, _, _) => ExecutionMode.NoExec,
        (_, true, _) => ExecutionMode.DryRun,
        (_, _, true) => ExecutionMode.AutoExec,
        _            => ExecutionMode.Ask,
    };

    var opts = new CliOptions(
        Prompt:     prompt,
        Provider:   providerOverride,
        Model:      parseResult.GetValue(modelOpt),
        NoContext:  parseResult.GetValue(noContextOpt),
        IncludeDir: parseResult.GetValue(includeDirOpt),
        ExecMode:   execMode,
        Verbose:    parseResult.GetValue(verboseOpt),
        ForceTools: parseResult.GetValue(forceToolsOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await handler.RunAsync(opts, cts.Token);
});

// ── config-Subkommando ────────────────────────────────────────────────────────

var configCommand = new Command("config", "Konfiguration lesen und setzen");

// config list
var configListCommand = new Command("list", "Alle Einstellungen anzeigen");
configListCommand.SetAction(async (_, _) =>
    Console.WriteLine(await configService.ListAsync()));

// config get <key>
var configGetKey = new Argument<string>("key") { Description = "Konfigurationsschlüssel" };
var configGetCommand = new Command("get", "Einen Wert lesen");
configGetCommand.Arguments.Add(configGetKey);
configGetCommand.SetAction(async (parseResult, _) =>
{
    try { Console.WriteLine(await configService.GetAsync(parseResult.GetValue(configGetKey)!)); }
    catch (ArgumentException ex) { Console.Error.WriteLine($"Fehler: {ex.Message}"); }
});

// config set <key> <value>
var configSetKey   = new Argument<string>("key")   { Description = "Konfigurationsschlüssel" };
var configSetValue = new Argument<string>("value") { Description = "Neuer Wert" };
var configSetCommand = new Command("set", "Einen Wert setzen");
configSetCommand.Arguments.Add(configSetKey);
configSetCommand.Arguments.Add(configSetValue);
configSetCommand.SetAction(async (parseResult, _) =>
{
    var key   = parseResult.GetValue(configSetKey)!;
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

// ── server-Subkommando ───────────────────────────────────────────────────────

var serverCommand = new Command("server", "Lokalen HTTP-Server mit Browser-UI starten");
var serverProviderOpt = new Option<string?>("--provider", "-p")
{
    Description = "LLM-Provider: 'ollama' oder 'cerebras' (überschreibt Config)"
};
var serverModelOpt = new Option<string?>("--model", "-m")
{
    Description = "Modellname (überschreibt Config)"
};
var serverNoContextOpt = new Option<bool>("--no-context")
{
    Description = "Kein Shell-Kontext mitschicken"
};
var serverIncludeDirOpt = new Option<bool>("--include-dir")
{
    Description = "Verzeichnisinhalt in den Kontext aufnehmen"
};
var serverAutoExecOpt = new Option<bool>("--auto-exec", "-y")
{
    Description = "Befehle ohne Bestätigung ausführen"
};
var serverDryRunOpt = new Option<bool>("--dry-run")
{
    Description = "Befehle anzeigen, aber nie ausführen"
};
var serverNoExecOpt = new Option<bool>("--no-exec")
{
    Description = "Keine Befehle ausführen (reiner Chat-Modus)"
};
var serverVerboseOpt = new Option<bool>("--verbose", "-v")
{
    Description = "Debug-Ausgaben anzeigen"
};
var serverForceToolsOpt = new Option<bool>("--force-tools")
{
    Description = "Tool-Calls erzwingen (tool_choice=bash)"
};
var serverPortOpt = new Option<int>("--port")
{
    Description = "Port für den Server-Modus",
    DefaultValueFactory = _ => 5050
};
var serverNoBrowserOpt = new Option<bool>("--no-browser")
{
    Description = "Browser beim Start nicht automatisch öffnen"
};
serverCommand.Options.Add(serverProviderOpt);
serverCommand.Options.Add(serverModelOpt);
serverCommand.Options.Add(serverNoContextOpt);
serverCommand.Options.Add(serverIncludeDirOpt);
serverCommand.Options.Add(serverAutoExecOpt);
serverCommand.Options.Add(serverDryRunOpt);
serverCommand.Options.Add(serverNoExecOpt);
serverCommand.Options.Add(serverVerboseOpt);
serverCommand.Options.Add(serverForceToolsOpt);
serverCommand.Options.Add(serverPortOpt);
serverCommand.Options.Add(serverNoBrowserOpt);

serverCommand.SetAction(async (parseResult, ct) =>
{
    var providerStr = parseResult.GetValue(serverProviderOpt);
    ProviderType? providerOverride = providerStr?.ToLowerInvariant() switch
    {
        "ollama"   => ProviderType.Ollama,
        "cerebras" => ProviderType.Cerebras,
        null       => null,
        var v      => throw new ArgumentException($"Unbekannter Provider '{v}'. Erlaubt: ollama, cerebras")
    };

    var autoExec = parseResult.GetValue(serverAutoExecOpt);
    var dryRun   = parseResult.GetValue(serverDryRunOpt);
    var noExec   = parseResult.GetValue(serverNoExecOpt);

    var execMode = (noExec, dryRun, autoExec) switch
    {
        (true, _, _) => ExecutionMode.NoExec,
        (_, true, _) => ExecutionMode.DryRun,
        (_, _, true) => ExecutionMode.AutoExec,
        _            => ExecutionMode.Ask,
    };

    var serverOptions = new ServerOptions(
        Port: parseResult.GetValue(serverPortOpt),
        NoBrowser: parseResult.GetValue(serverNoBrowserOpt),
        Provider: providerOverride,
        Model: parseResult.GetValue(serverModelOpt),
        NoContext: parseResult.GetValue(serverNoContextOpt),
        IncludeDir: parseResult.GetValue(serverIncludeDirOpt),
        ExecMode: execMode,
        Verbose: parseResult.GetValue(serverVerboseOpt),
        ForceTools: parseResult.GetValue(serverForceToolsOpt));

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await serverHost.RunAsync(serverOptions, cts.Token);
});

rootCommand.Subcommands.Add(serverCommand);

// ── Start ─────────────────────────────────────────────────────────────────────

return await rootCommand.Parse(args).InvokeAsync();
