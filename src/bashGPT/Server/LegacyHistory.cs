using System.Text.Json;
using BashGPT.Providers;

namespace BashGPT.Server;

internal sealed record HistoryItem(string Role, string Content);

/// <summary>
/// Verwaltet den globalen In-Memory-Gesprächsverlauf und dessen optionale Datei-Persistenz.
/// Wird als Fallback genutzt, wenn kein SessionStore verfügbar ist.
/// </summary>
internal sealed class LegacyHistory(string? filePath = null)
{
    private readonly List<ChatMessage> _history = [];
    private readonly object _lock = new();

    public async Task LoadFromFileAsync()
    {
        if (filePath is null || !File.Exists(filePath)) return;
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var items = JsonSerializer.Deserialize<List<HistoryItem>>(json, JsonDefaults.Options) ?? [];
            var messages = items
                .Select(item => item.Role switch
                {
                    "user"      => new ChatMessage(ChatRole.User,      item.Content),
                    "assistant" => new ChatMessage(ChatRole.Assistant,  item.Content),
                    _           => (ChatMessage?)null
                })
                .Where(m => m is not null)
                .Select(m => m!)
                .ToList();
            lock (_lock)
            {
                _history.Clear();
                _history.AddRange(messages);
                if (_history.Count > 40)
                    _history.RemoveRange(0, _history.Count - 40);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Beschädigte Datei ignorieren – Neustart mit leerem Verlauf
            _ = ex;
        }
    }

    public async Task PersistAsync()
    {
        if (filePath is null) return;
        try
        {
            List<HistoryItem> items;
            lock (_lock)
                items = _history.Select(m => new HistoryItem(m.RoleString, m.Content)).ToList();
            var dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);
            var serialized = JsonSerializer.Serialize(items, JsonDefaults.Options);
            await File.WriteAllTextAsync(filePath, serialized);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Schreibfehler ignorieren
            _ = ex;
        }
    }

    public IReadOnlyList<HistoryItem> GetItems()
    {
        lock (_lock)
            return _history.Select(m => new HistoryItem(m.RoleString, m.Content)).ToList();
    }

    public IReadOnlyList<ChatMessage> GetSnapshot()
    {
        lock (_lock)
            return _history.ToList();
    }

    public void Append(ChatMessage message)
    {
        lock (_lock)
        {
            _history.Add(message);
            if (_history.Count > 40)
                _history.RemoveRange(0, _history.Count - 40);
        }
    }

    public void Clear()
    {
        lock (_lock)
            _history.Clear();
    }
}
