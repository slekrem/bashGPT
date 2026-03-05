using System.Text.Json;
using System.Text.Json.Serialization;

namespace BashGPT.Agents;

/// <summary>
/// Verwaltet Agent-Definitionen in ~/.config/bashgpt/agents.json.
/// Thread-safe via SemaphoreSlim, atomisches Schreiben via Temp-Datei.
/// </summary>
public sealed class AgentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _agentsFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AgentStore(string agentsFile)
    {
        _agentsFile = agentsFile;
    }

    public async Task<List<AgentRecord>> LoadAllAsync()
    {
        await _lock.WaitAsync();
        try { return (await ReadFileInternalAsync()).Agents; }
        finally { _lock.Release(); }
    }

    public async Task<AgentRecord?> LoadAsync(string idOrName)
    {
        await _lock.WaitAsync();
        try
        {
            var file = await ReadFileInternalAsync();
            return file.Agents.FirstOrDefault(a => a.Id == idOrName || a.Name == idOrName);
        }
        finally { _lock.Release(); }
    }

    public async Task UpsertAsync(AgentRecord agent)
    {
        await _lock.WaitAsync();
        try
        {
            var file = await ReadFileInternalAsync();
            var idx = file.Agents.FindIndex(a => a.Id == agent.Id);
            if (idx >= 0)
                file.Agents[idx] = agent;
            else
                file.Agents.Add(agent);
            await WriteFileInternalAsync(file);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(string idOrName)
    {
        await _lock.WaitAsync();
        try
        {
            var file = await ReadFileInternalAsync();
            file.Agents.RemoveAll(a => a.Id == idOrName || a.Name == idOrName);
            await WriteFileInternalAsync(file);
        }
        finally { _lock.Release(); }
    }

    private async Task<AgentsFile> ReadFileInternalAsync()
    {
        if (!File.Exists(_agentsFile))
            return new AgentsFile();

        try
        {
            var json = await File.ReadAllTextAsync(_agentsFile);
            return JsonSerializer.Deserialize<AgentsFile>(json, JsonOptions) ?? new AgentsFile();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AgentsFile();
        }
    }

    private async Task WriteFileInternalAsync(AgentsFile file)
    {
        var dir = System.IO.Path.GetDirectoryName(_agentsFile)!;
        Directory.CreateDirectory(dir);

        var tmp = _agentsFile + ".tmp";
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, _agentsFile, overwrite: true);
    }
}
