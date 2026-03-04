using System.CommandLine;
using BashGPT.Agents;

namespace BashGPT.Cli;

public static class AgentCommands
{
    public static Command Build(AgentStore store)
    {
        var agentCommand = new Command("agent", "Persistente Agenten für zyklische Checks verwalten");

        agentCommand.Subcommands.Add(BuildAddCommand(store));
        agentCommand.Subcommands.Add(BuildListCommand(store));
        agentCommand.Subcommands.Add(BuildStatusCommand(store));
        agentCommand.Subcommands.Add(BuildPauseCommand(store));
        agentCommand.Subcommands.Add(BuildResumeCommand(store));
        agentCommand.Subcommands.Add(BuildRemoveCommand(store));
        agentCommand.Subcommands.Add(BuildRunCommand(store));

        return agentCommand;
    }

    private static Command BuildAddCommand(AgentStore store)
    {
        var addCommand = new Command("add", "Neuen Agenten hinzufügen");

        addCommand.Subcommands.Add(BuildAddGitCommand(store));
        addCommand.Subcommands.Add(BuildAddHttpCommand(store));
        addCommand.Subcommands.Add(BuildAddBitcoinPriceCommand(store));

        return addCommand;
    }

    private static Command BuildAddGitCommand(AgentStore store)
    {
        var nameOpt = new Option<string>("--name") { Description = "Name des Agenten", Required = true };
        var pathOpt = new Option<string>("--path") { Description = "Arbeitsverzeichnis (Standard: .)", DefaultValueFactory = _ => "." };
        var everyOpt = new Option<int>("--every") { Description = "Prüfintervall in Sekunden (Standard: 30)", DefaultValueFactory = _ => 30 };

        var cmd = new Command("git", "Git-Status-Agent hinzufügen");
        cmd.Options.Add(nameOpt);
        cmd.Options.Add(pathOpt);
        cmd.Options.Add(everyOpt);

        cmd.SetAction(async (parseResult, _) =>
        {
            var name = parseResult.GetValue(nameOpt)!;
            var path = System.IO.Path.GetFullPath(parseResult.GetValue(pathOpt)!);
            var interval = parseResult.GetValue(everyOpt);

            var agent = new AgentRecord
            {
                Id = GenerateId(),
                Name = name,
                Type = AgentCheckType.GitStatus,
                Path = path,
                IntervalSeconds = interval,
                IsActive = true,
            };

            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{name}' ({agent.Id}) hinzugefügt. Pfad: {path}, Intervall: {interval}s");
        });

        return cmd;
    }

    private static Command BuildAddHttpCommand(AgentStore store)
    {
        var nameOpt = new Option<string>("--name") { Description = "Name des Agenten", Required = true };
        var urlOpt = new Option<string>("--url") { Description = "Ziel-URL", Required = true };
        var everyOpt = new Option<int>("--every") { Description = "Prüfintervall in Sekunden (Standard: 60)", DefaultValueFactory = _ => 60 };

        var cmd = new Command("http", "HTTP-Status-Agent hinzufügen");
        cmd.Options.Add(nameOpt);
        cmd.Options.Add(urlOpt);
        cmd.Options.Add(everyOpt);

        cmd.SetAction(async (parseResult, _) =>
        {
            var name = parseResult.GetValue(nameOpt)!;
            var url = parseResult.GetValue(urlOpt)!;
            var interval = parseResult.GetValue(everyOpt);

            var agent = new AgentRecord
            {
                Id = GenerateId(),
                Name = name,
                Type = AgentCheckType.HttpStatus,
                Url = url,
                IntervalSeconds = interval,
                IsActive = true,
            };

            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{name}' ({agent.Id}) hinzugefügt. URL: {url}, Intervall: {interval}s");
        });

        return cmd;
    }

    private static Command BuildAddBitcoinPriceCommand(AgentStore store)
    {
        var nameOpt = new Option<string>("--name") { Description = "Name des Agenten", Required = true };
        var everyOpt = new Option<int>("--every") { Description = "Prüfintervall in Sekunden (Standard: 30)", DefaultValueFactory = _ => 30 };

        var cmd = new Command("bitcoin-price", "Bitcoin-Preis-Agent hinzufügen (mempool.space)");
        cmd.Options.Add(nameOpt);
        cmd.Options.Add(everyOpt);

        cmd.SetAction(async (parseResult, _) =>
        {
            var name = parseResult.GetValue(nameOpt)!;
            var interval = parseResult.GetValue(everyOpt);

            var agent = new AgentRecord
            {
                Id = GenerateId(),
                Name = name,
                Type = AgentCheckType.BitcoinPrice,
                Url = "https://mempool.space/api/v1/prices",
                IntervalSeconds = interval,
                IsActive = true,
            };

            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{name}' ({agent.Id}) hinzugefügt. Intervall: {interval}s");
        });

        return cmd;
    }

    private static Command BuildListCommand(AgentStore store)
    {
        var cmd = new Command("list", "Alle Agenten anzeigen");

        cmd.SetAction(async (_, _) =>
        {
            var agents = await store.LoadAllAsync();

            if (agents.Count == 0)
            {
                Console.WriteLine("Keine Agenten konfiguriert.");
                return;
            }

            foreach (var a in agents)
            {
                var status = a.IsActive ? "aktiv" : "pausiert";
                var lastRun = a.LastRun?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "nie";
                var target = a.Type == AgentCheckType.GitStatus ? a.Path : a.Url;
                Console.WriteLine($"  {a.Id}  {a.Name,-20}  {a.Type,-12}  {status,-8}  alle {a.IntervalSeconds}s  zuletzt: {lastRun}  → {target}");
            }
        });

        return cmd;
    }

    private static Command BuildStatusCommand(AgentStore store)
    {
        var nameOrIdArg = new Argument<string>("name-or-id") { Description = "Name oder ID des Agenten" };
        var cmd = new Command("status", "Status eines Agenten anzeigen");
        cmd.Arguments.Add(nameOrIdArg);

        cmd.SetAction(async (parseResult, _) =>
        {
            var key = parseResult.GetValue(nameOrIdArg)!;
            var agent = await store.LoadAsync(key);

            if (agent is null)
            {
                Console.Error.WriteLine($"Agent '{key}' nicht gefunden.");
                return;
            }

            Console.WriteLine($"ID:            {agent.Id}");
            Console.WriteLine($"Name:          {agent.Name}");
            Console.WriteLine($"Typ:           {agent.Type}");
            Console.WriteLine($"Aktiv:         {agent.IsActive}");
            Console.WriteLine($"Intervall:     {agent.IntervalSeconds}s");
            Console.WriteLine($"Ziel:          {agent.Path ?? agent.Url ?? "-"}");
            Console.WriteLine($"Letzter Run:   {agent.LastRun?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "nie"}");
            Console.WriteLine($"Letzter Hash:  {agent.LastHash ?? "-"}");
            Console.WriteLine($"Letzter Stand: {agent.LastMessage ?? "-"}");
            Console.WriteLine($"Erfolgreich:   {agent.LastCheckSucceeded}");
            Console.WriteLine($"Fehler:        {agent.FailureCount}");
        });

        return cmd;
    }

    private static Command BuildPauseCommand(AgentStore store)
    {
        var nameOrIdArg = new Argument<string>("name-or-id") { Description = "Name oder ID des Agenten" };
        var cmd = new Command("pause", "Agenten pausieren");
        cmd.Arguments.Add(nameOrIdArg);

        cmd.SetAction(async (parseResult, _) =>
        {
            var key = parseResult.GetValue(nameOrIdArg)!;
            var agent = await store.LoadAsync(key);

            if (agent is null)
            {
                Console.Error.WriteLine($"Agent '{key}' nicht gefunden.");
                return;
            }

            agent.IsActive = false;
            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{agent.Name}' pausiert.");
        });

        return cmd;
    }

    private static Command BuildResumeCommand(AgentStore store)
    {
        var nameOrIdArg = new Argument<string>("name-or-id") { Description = "Name oder ID des Agenten" };
        var cmd = new Command("resume", "Agenten fortsetzen");
        cmd.Arguments.Add(nameOrIdArg);

        cmd.SetAction(async (parseResult, _) =>
        {
            var key = parseResult.GetValue(nameOrIdArg)!;
            var agent = await store.LoadAsync(key);

            if (agent is null)
            {
                Console.Error.WriteLine($"Agent '{key}' nicht gefunden.");
                return;
            }

            agent.IsActive = true;
            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{agent.Name}' fortgesetzt.");
        });

        return cmd;
    }

    private static Command BuildRemoveCommand(AgentStore store)
    {
        var nameOrIdArg = new Argument<string>("name-or-id") { Description = "Name oder ID des Agenten" };
        var cmd = new Command("remove", "Agenten entfernen");
        cmd.Arguments.Add(nameOrIdArg);

        cmd.SetAction(async (parseResult, _) =>
        {
            var key = parseResult.GetValue(nameOrIdArg)!;
            await store.DeleteAsync(key);
            Console.WriteLine($"Agent '{key}' entfernt.");
        });

        return cmd;
    }

    private static Command BuildRunCommand(AgentStore store)
    {
        var cmd = new Command("run", "Alle aktiven Agenten starten (Ctrl+C zum Beenden)");

        cmd.SetAction(async (_, ct) =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            var runner = new AgentRunner(store, [new GitStatusCheck(), new HttpStatusCheck(), new BitcoinPriceCheck()]);

            Console.WriteLine("Agent-Runner gestartet. Ctrl+C zum Beenden.");
            try
            {
                await runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Agent-Runner beendet.");
            }
        });

        return cmd;
    }

    private static string GenerateId()
    {
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return $"ag-{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
