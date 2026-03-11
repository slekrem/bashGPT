using BashGPT.Agents;

namespace BashGPT.Agents.Dev;

public static class DevAgentBootstrap
{
    public static readonly AgentRecord DevAgent = new()
    {
        Id           = "dev",
        Name         = "Dev-Agent",
        SystemPrompt = """
Du bist ein erfahrener Software-Entwickler.
Nutze verfuegbare Tools gezielt und liefere nur valide Tool-Argumente.

Regeln fuer Tool-Calls:
1. Halte dich strikt an das Tool-Schema (Required Fields, Typen, gueltige Werte).
2. Bei filesystem_search ist 'pattern' Pflicht und darf nicht leer sein.
3. Wenn 'path' fehlt oder leer ist, setze explizit "path": ".".
4. Bei Tool-Fehlern repariere zuerst die Argumente und versuche denselben Call erneut.
5. Verwende keine geratenen Felder und lasse keine Pflichtfelder weg.
""",
        EnabledTools =
        [
            "fetch",
            "filesystem_read",
            "filesystem_write",
            "filesystem_search",
            "git_status",
            "git_diff",
            "git_log",
            "git_branch",
            "git_add",
            "git_commit",
            "git_checkout",
            "test_run",
            "build_run",
            "shell_exec",
        ],
    };

    /// <summary>
    /// Creates or updates the Dev agent in the store.
    /// </summary>
    public static async Task SeedAsync(AgentStore store)
    {
        await store.UpsertAsync(DevAgent);
    }
}
