using BashGPT.Providers;
using System.Reflection;

namespace BashGPT.Core.Tests.Providers;

public class LlmRateLimiterTests
{
    // ── MaxRequestsPerMinute ──────────────────────────────────────────────────

    [Fact]
    public async Task WaitAsync_UnderLimit_CompletesImmediately()
    {
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 5, minIntervalMs: 0);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < 5; i++)
            await limiter.WaitAsync();

        sw.Stop();
        // Alle 5 Slots sofort verfügbar → kein spürbares Warten
        Assert.True(sw.ElapsedMilliseconds < 500);
    }

    [Fact]
    public async Task WaitAsync_OverLimit_BlocksUntilWindowExpires()
    {
        // Sehr kurzes Fenster simulieren: 2 req/min mit kurzem Warten
        // Wir nutzen einen Limiter mit 2 req und testen, dass der 3. blockiert.
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 2, minIntervalMs: 0);

        await limiter.WaitAsync();
        await limiter.WaitAsync();

        // 3. Aufruf muss blockieren – wir erwarten, dass er nicht sofort zurückkommt
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Wir brechen nach kurzer Zeit ab – der Limiter soll warten (bis zu 60 s)
        var cancelled = false;
        using var cancel = new CancellationTokenSource(200);
        try { await limiter.WaitAsync(cancel.Token); }
        catch (OperationCanceledException) { cancelled = true; }

        Assert.True(cancelled, "Der Rate-Limiter hätte den dritten Aufruf blockieren sollen.");
        sw.Stop();
    }

    [Fact]
    public async Task WaitAsync_DisabledLimit_NeverBlocks()
    {
        // MaxRequestsPerMinute = 0 → kein Sliding-Window-Limit
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 0, minIntervalMs: 0);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < 50; i++)
            await limiter.WaitAsync();

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, "Ohne Limit sollten 50 Aufrufe nicht blockieren.");
    }

    [Fact]
    public async Task WaitAsync_OldWindowEntries_AreDequeued()
    {
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 1, minIntervalMs: 0);
        var windowField = typeof(LlmRateLimiter).GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(windowField);

        var window = Assert.IsType<Queue<DateTime>>(windowField!.GetValue(limiter));
        window.Enqueue(DateTime.UtcNow.AddMinutes(-2));

        await limiter.WaitAsync();

        Assert.Single(window);
        Assert.True(window.Peek() > DateTime.UtcNow.AddSeconds(-5));
    }

    // ── MinIntervalMs ────────────────────────────────────────────────────────

    [Fact]
    public async Task WaitAsync_MinInterval_EnforcesDelay()
    {
        const int intervalMs = 200;
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 0, minIntervalMs: intervalMs);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await limiter.WaitAsync(); // 1. – sofort
        await limiter.WaitAsync(); // 2. – muss >= intervalMs warten
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= intervalMs - 20,
            $"Mindestabstand nicht eingehalten: {sw.ElapsedMilliseconds} ms < {intervalMs} ms");
    }

    [Fact]
    public async Task WaitAsync_NoMinInterval_FirstCallImmediate()
    {
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 0, minIntervalMs: 0);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await limiter.WaitAsync();
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task WaitAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 1, minIntervalMs: 0);
        await limiter.WaitAsync(); // Slot verbrauchen

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    // ── RateLimitedLlmProvider ───────────────────────────────────────────────

    [Fact]
    public async Task RateLimitedProvider_CallsInnerProvider()
    {
        var inner = new FakeLlmProvider();
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 10, minIntervalMs: 0);
        var provider = new RateLimitedLlmProvider(inner, limiter);

        await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")]);

        Assert.Equal(1, inner.CompleteCallCount);
    }

    [Fact]
    public async Task RateLimitedProvider_ChatAsync_CallsInnerProvider()
    {
        var inner = new FakeLlmProvider();
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 10, minIntervalMs: 0);
        var provider = new RateLimitedLlmProvider(inner, limiter);

        var response = await provider.ChatAsync(new LlmChatRequest(
            Messages: [new ChatMessage(ChatRole.User, "Hi")]));

        Assert.Equal("ok", response.Content);
        Assert.Equal(1, inner.ChatCallCount);
    }

    [Fact]
    public async Task RateLimitedProvider_StreamAsync_CallsInnerProvider_AndYieldsTokens()
    {
        var inner = new FakeLlmProvider();
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 10, minIntervalMs: 0);
        var provider = new RateLimitedLlmProvider(inner, limiter);

        var tokens = new List<string>();
        await foreach (var token in provider.StreamAsync([new ChatMessage(ChatRole.User, "Hi")]))
            tokens.Add(token);

        Assert.Equal(1, inner.StreamCallCount);
        Assert.Equal(["a", "b"], tokens);
    }

    [Fact]
    public void RateLimitedProvider_ExposesInnerNameAndModel()
    {
        var inner = new FakeLlmProvider();
        var limiter = new LlmRateLimiter(maxRequestsPerMinute: 10, minIntervalMs: 0);
        var provider = new RateLimitedLlmProvider(inner, limiter);

        Assert.Equal(inner.Name, provider.Name);
        Assert.Equal(inner.Model, provider.Model);
    }

    // ── Hilfklassen ──────────────────────────────────────────────────────────

    private sealed class FakeLlmProvider : ILlmProvider
    {
        public string Name  => "fake";
        public string Model => "fake-model";
        public int CompleteCallCount { get; private set; }
        public int ChatCallCount { get; private set; }
        public int StreamCallCount { get; private set; }

        public Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
        {
            ChatCallCount++;
            return Task.FromResult(new LlmChatResponse("ok", []));
        }

        public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        {
            CompleteCallCount++;
            return Task.FromResult("ok");
        }

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> StreamAsync(
            IEnumerable<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            StreamCallCount++;
            yield return "a";
            yield return "b";
        }
#pragma warning restore CS1998
    }
}
