using BashGPT.Agents;

namespace BashGPT.Agents.Shell;

public static class ShellAgentBootstrap
{
    public static readonly AgentRecord ShellAgent = new()
    {
        Id           = "shell",
        Name         = "Shell-Agent",
        SystemPrompt = "Du bist ein erfahrener Shell-Assistent. Nutze das shell_exec-Tool, um Befehle auszufuehren und dem Benutzer bei Terminal-Aufgaben zu helfen. Erklaere, was du tust, und zeige Ergebnisse uebersichtlich.",
        EnabledTools = ["shell_exec"],
    };

    public static async Task SeedAsync(AgentStore store)
    {
        await store.UpsertAsync(ShellAgent);
    }
}
