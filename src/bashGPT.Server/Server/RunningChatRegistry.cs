using System.Collections.Concurrent;

namespace BashGPT.Server;

internal sealed class RunningChatRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new(StringComparer.Ordinal);

    public bool Register(string requestId, CancellationTokenSource cts)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return false;

        return _running.TryAdd(requestId, cts);
    }

    public bool Cancel(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return false;

        if (!_running.TryGetValue(requestId, out var cts))
            return false;

        try { cts.Cancel(); } catch { /* best effort */ }
        return true;
    }

    public void Unregister(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return;

        _running.TryRemove(requestId, out _);
    }
}
