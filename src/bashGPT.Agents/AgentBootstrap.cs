namespace BashGPT.Agents;

public static class AgentBootstrap
{
    public static string GetConfigDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "bashgpt");

    public static AgentStore CreateAgentStore(string? configDir = null)
    {
        var baseDir = configDir ?? GetConfigDir();
        var agentsFile = Path.Combine(baseDir, "agents.json");
        return new AgentStore(agentsFile);
    }
}
