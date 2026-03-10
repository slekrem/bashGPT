using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Agents;

/// <summary>
/// Verwaltet Agent-Definitionen, je eine Datei pro Agent:
/// {agentsDir}/{id}/config.json
/// </summary>
public sealed class AgentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _agentsDir;

    public AgentStore(string agentsDir)
    {
        _agentsDir = agentsDir;
    }

    public async Task<List<AgentRecord>> LoadAllAsync()
    {
        if (!Directory.Exists(_agentsDir))
            return [];

        var result = new List<AgentRecord>();
        foreach (var dir in Directory.GetDirectories(_agentsDir))
        {
            var agent = await LoadFromDirAsync(dir);
            if (agent is not null)
                result.Add(agent);
        }
        return result;
    }

    public async Task<AgentRecord?> LoadAsync(string idOrName)
    {
        // Try direct ID lookup first
        var idDir = Path.Combine(_agentsDir, idOrName);
        if (Directory.Exists(idDir))
        {
            var agent = await LoadFromDirAsync(idDir);
            if (agent is not null) return agent;
        }

        // Fall back to name search
        var all = await LoadAllAsync();
        return all.FirstOrDefault(a => a.Name == idOrName);
    }

    public async Task UpsertAsync(AgentRecord agent)
    {
        var dir = Path.Combine(_agentsDir, agent.Id);
        Directory.CreateDirectory(dir);

        var file = Path.Combine(dir, "config.json");
        var tmp  = file + ".tmp";
        var json = JsonSerializer.Serialize(agent, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, file, overwrite: true);
    }

    public Task DeleteAsync(string idOrName)
    {
        // Try direct ID
        var idDir = Path.Combine(_agentsDir, idOrName);
        if (Directory.Exists(idDir))
        {
            Directory.Delete(idDir, recursive: true);
            return Task.CompletedTask;
        }

        // Fall back to name search (synchronous scan is fine for delete)
        if (!Directory.Exists(_agentsDir))
            return Task.CompletedTask;

        foreach (var dir in Directory.GetDirectories(_agentsDir))
        {
            var file = Path.Combine(dir, "config.json");
            if (!File.Exists(file)) continue;
            try
            {
                var json  = File.ReadAllText(file);
                var agent = JsonSerializer.Deserialize<AgentRecord>(json, JsonOptions);
                if (agent?.Name == idOrName)
                {
                    Directory.Delete(dir, recursive: true);
                    break;
                }
            }
            catch { /* skip unreadable entries */ }
        }
        return Task.CompletedTask;
    }

    private static async Task<AgentRecord?> LoadFromDirAsync(string dir)
    {
        var file = Path.Combine(dir, "config.json");
        if (!File.Exists(file)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(file);
            return JsonSerializer.Deserialize<AgentRecord>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
