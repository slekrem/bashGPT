namespace BashGPT.Providers;

/// <summary>
/// Sliding-Window-Rate-Limiter für LLM-Aufrufe.
/// Begrenzt die Anzahl der Anfragen pro Minute und erzwingt optional
/// einen Mindestabstand zwischen aufeinanderfolgenden Aufrufen.
/// Thread-sicher.
/// </summary>
public sealed class LlmRateLimiter
{
    private readonly int _maxRequestsPerMinute;
    private readonly int _minIntervalMs;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Queue<DateTime> _window = new();
    private DateTime _lastRequestTime = DateTime.MinValue;

    public LlmRateLimiter(int maxRequestsPerMinute, int minIntervalMs = 0)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute;
        _minIntervalMs        = minIntervalMs;
    }

    /// <summary>
    /// Wartet so lange, bis ein neuer LLM-Aufruf nach den konfigurierten
    /// Rate-Limit-Regeln erlaubt ist.
    /// </summary>
    public async Task WaitAsync(CancellationToken ct = default)
    {
        while (true)
        {
            TimeSpan? delay = null;

            await _lock.WaitAsync(ct);
            try
            {
                var now = DateTime.UtcNow;

                // Mindestabstand zwischen zwei Aufrufen
                if (_minIntervalMs > 0)
                {
                    var intervalDelay = _lastRequestTime.AddMilliseconds(_minIntervalMs) - now;
                    if (intervalDelay > TimeSpan.Zero)
                    {
                        delay = intervalDelay;
                    }
                }

                // Sliding-Window: max. N Anfragen pro Minute
                if (delay is null && _maxRequestsPerMinute > 0)
                {
                    var cutoff = now.AddMinutes(-1);
                    while (_window.Count > 0 && _window.Peek() <= cutoff)
                        _window.Dequeue();

                    if (_window.Count >= _maxRequestsPerMinute)
                        delay = _window.Peek().AddMinutes(1) - now + TimeSpan.FromMilliseconds(50);
                }

                if (delay is null)
                {
                    _window.Enqueue(now);
                    _lastRequestTime = now;
                    return;
                }
            }
            finally
            {
                _lock.Release();
            }

            await Task.Delay(delay.Value, ct);
        }
    }
}
