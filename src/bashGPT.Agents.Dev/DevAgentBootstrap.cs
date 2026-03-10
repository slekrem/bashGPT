using BashGPT.Agents;

namespace BashGPT.Agents.Dev;

public static class DevAgentBootstrap
{
    public static readonly AgentRecord DevAgent = new()
    {
        Id           = "dev",
        Name         = "Dev-Agent",
        SystemPrompt =
            "Du bist ein erfahrener Software-Entwickler. Verwende die verfügbaren Tools, um Code zu lesen, " +
            "zu schreiben, Git-Operationen durchzuführen, Tests auszuführen und Builds zu starten. " +
            "Arbeite präzise, erkläre deine Schritte und zeige Ergebnisse übersichtlich.",
        EnabledTools =
        [
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
        ],
    };

    /// <summary>
    /// Legt den Dev-Agenten im Store an, falls er noch nicht existiert.
    /// </summary>
    public static async Task SeedAsync(AgentStore store)
    {
        var existing = await store.LoadAsync(DevAgent.Id);
        if (existing is null)
            await store.UpsertAsync(DevAgent);
    }
}
