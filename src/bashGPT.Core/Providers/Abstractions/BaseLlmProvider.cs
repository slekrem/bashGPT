using bashGPT.Core.Models.Providers;

namespace bashGPT.Core.Providers.Abstractions;

/// <summary>
/// Shared base functionality for LLM providers:
/// HTTP client creation, exception wrapping, and CompleteAsync.
/// </summary>
public abstract class BaseLlmProvider(HttpClient? httpClient = null) : ILlmProvider
{
    protected readonly HttpClient Http = httpClient ?? CreateDefaultHttpClient();

    public abstract string Name  { get; }
    public abstract string Model { get; }

    public abstract Task<LlmChatResponse> ChatAsync(
        LlmChatRequest request,
        CancellationToken ct = default);

    public abstract IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);

    public async Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var token in StreamAsync(messages, ct))
            sb.Append(token);
        return sb.ToString();
    }

    protected static HttpClient CreateDefaultHttpClient()
    {
        var handler = new SocketsHttpHandler { UseCookies = false };
        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
    }

    protected static LlmProviderException WrapHttpException(
        HttpRequestException ex, string baseUrl)
        => new($"Unreachable ({baseUrl}): {ex.Message}", ex);

    protected static LlmProviderException WrapTimeoutException(
        TaskCanceledException ex, string context)
        => new($"Timed out while connecting to {context}.", ex);
}
