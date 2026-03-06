using System.Runtime.CompilerServices;

namespace BashGPT.Providers;

/// <summary>
/// Dekorator für <see cref="ILlmProvider"/>, der alle Aufrufe durch
/// einen <see cref="LlmRateLimiter"/> schleust, bevor sie an den
/// eigentlichen Provider weitergegeben werden.
/// </summary>
public sealed class RateLimitedLlmProvider(ILlmProvider inner, LlmRateLimiter rateLimiter) : ILlmProvider
{
    public string Name  => inner.Name;
    public string Model => inner.Model;

    public async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        await rateLimiter.WaitAsync(ct);
        return await inner.ChatAsync(request, ct);
    }

    public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        await rateLimiter.WaitAsync(ct);
        return await inner.CompleteAsync(messages, ct);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await rateLimiter.WaitAsync(ct);
        await foreach (var token in inner.StreamAsync(messages, ct))
            yield return token;
    }
}
