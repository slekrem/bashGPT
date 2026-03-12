namespace BashGPT.Agents;

/// <summary>
/// Basismodul fuer Agent-Infrastruktur.
/// Enthält absichtlich keine konkreten Agent-Definitionen.
/// Konkrete Agenten werden in separaten Projekten gepflegt
/// (z. B. bashGPT.Agents.Dev, bashGPT.Agents.Shell).
/// </summary>
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
}
