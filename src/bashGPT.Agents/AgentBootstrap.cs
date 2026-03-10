namespace BashGPT.Agents;

public static class AgentBootstrap
{
    public static string GetConfigDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "bashgpt");

    public static AgentStore CreateAgentStore(string? configDir = null)
    {
        var baseDir   = configDir ?? GetConfigDir();
        var agentsDir = Path.Combine(baseDir, "agents");
        return new AgentStore(agentsDir);
    }

    /// <summary>
    /// Im Code registrierte Built-in-Agenten, die beim Start automatisch verfügbar sind.
    /// </summary>
    public static readonly IReadOnlyList<AgentRecord> BuiltInAgents =
    [
        new AgentRecord
        {
            Id           = "shell",
            Name         = "Shell-Agent",
            SystemPrompt = "Du bist ein erfahrener Shell-Assistent. Nutze das shell_exec-Tool, um Befehle auszuführen und dem Benutzer bei Terminal-Aufgaben zu helfen. Erkläre, was du tust, und zeige Ergebnisse übersichtlich.",
            EnabledTools = ["shell_exec"],
        },
    ];

    /// <summary>
    /// Legt Built-in-Agenten im Store an, falls sie noch nicht existieren.
    /// </summary>
    public static async Task SeedBuiltInAgentsAsync(AgentStore store)
    {
        var existing    = await store.LoadAllAsync();
        var existingIds = existing.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var agent in BuiltInAgents)
        {
            if (!existingIds.Contains(agent.Id))
                await store.UpsertAsync(agent);
        }
    }
}
