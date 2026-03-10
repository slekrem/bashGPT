using System.CommandLine;
using BashGPT.Agents;

namespace BashGPT.Cli;

public static class AgentCommands
{
    public static Command Build(AgentStore store)
    {
        var agentCommand = new Command("agent", "KI-Agenten verwalten");

        agentCommand.Subcommands.Add(BuildAddCommand(store));
        agentCommand.Subcommands.Add(BuildListCommand(store));
        agentCommand.Subcommands.Add(BuildRemoveCommand(store));
        agentCommand.Subcommands.Add(BuildSetPromptCommand(store));

        return agentCommand;
    }

    private static Command BuildAddCommand(AgentStore store)
    {
        var nameOpt   = new Option<string>("--name")   { Description = "Name des Agenten", Required = true };
        var promptOpt = new Option<string?>("--system-prompt") { Description = "System-Prompt des Agenten" };

        var cmd = new Command("add", "Neuen Agenten hinzufügen");
        cmd.Options.Add(nameOpt);
        cmd.Options.Add(promptOpt);

        cmd.SetAction(async (parseResult, _) =>
        {
            var agent = new AgentRecord
            {
                Id           = GenerateId(),
                Name         = parseResult.GetValue(nameOpt)!.Trim(),
                SystemPrompt = parseResult.GetValue(promptOpt)?.Trim(),
            };

            await store.UpsertAsync(agent);
            Console.WriteLine($"Agent '{agent.Name}' ({agent.Id}) hinzugefügt.");
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
                Console.WriteLine($"  {a.Id}  {a.Name,-20}  Prompt: {(a.SystemPrompt is null ? "(default)" : a.SystemPrompt[..Math.Min(40, a.SystemPrompt.Length)])}");
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

    private static Command BuildSetPromptCommand(AgentStore store)
    {
        var nameOrIdArg = new Argument<string>("name-or-id") { Description = "Name oder ID des Agenten" };
        var promptArg   = new Argument<string>("prompt") { Description = "Neue System-Prompt (leer zum Zurücksetzen)" };
        promptArg.Arity = ArgumentArity.ZeroOrOne;

        var cmd = new Command("set-prompt", "System-Prompt eines Agenten setzen oder zurücksetzen");
        cmd.Arguments.Add(nameOrIdArg);
        cmd.Arguments.Add(promptArg);

        cmd.SetAction(async (parseResult, _) =>
        {
            var key   = parseResult.GetValue(nameOrIdArg)!;
            var agent = await store.LoadAsync(key);

            if (agent is null)
            {
                Console.Error.WriteLine($"Agent '{key}' nicht gefunden.");
                return;
            }

            var prompt = parseResult.GetValue(promptArg);
            agent.SystemPrompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt;
            await store.UpsertAsync(agent);

            Console.WriteLine(agent.SystemPrompt is null
                ? $"System-Prompt von '{agent.Name}' zurückgesetzt."
                : $"System-Prompt von '{agent.Name}' aktualisiert.");
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
