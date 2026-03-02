using BashGPT.Providers;
using BashGPT;

namespace BashGPT.Server;

internal sealed record HistoryItem(string Role, string Content);

/// <summary>
/// Verwaltet den globalen In-Memory-Gesprächsverlauf.
/// Wird als Fallback genutzt, wenn kein SessionStore verfügbar ist.
/// </summary>
internal sealed class LegacyHistory
{
    private readonly List<ChatMessage> _history = [];
    private readonly object _lock = new();

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
            if (_history.Count > AppDefaults.MaxHistoryMessages)
                _history.RemoveRange(0, _history.Count - AppDefaults.MaxHistoryMessages);
        }
    }

    public void Clear()
    {
        lock (_lock)
            _history.Clear();
    }
}
