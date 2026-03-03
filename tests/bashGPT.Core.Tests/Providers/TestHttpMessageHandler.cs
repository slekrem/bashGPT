using System.Net;

namespace BashGPT.Tests.Providers;

/// <summary>
/// Gibt bei jedem Aufruf dieselbe Antwort zurück.
/// </summary>
internal sealed class TestHttpMessageHandler(
    string responseBody,
    HttpStatusCode statusCode = HttpStatusCode.OK,
    string contentType = "application/x-ndjson") : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody,
                System.Text.Encoding.UTF8, contentType)
        };
        return response;
    }
}

/// <summary>
/// Gibt eine Sequenz vorgefertigter <see cref="HttpResponseMessage"/>-Objekte zurück.
/// Nützlich für Retry-Tests: erste Anfrage → 429, zweite → 200 etc.
/// </summary>
internal sealed class SequentialHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;
    public int CallCount { get; private set; }

    public SequentialHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return _responses.TryDequeue(out var response)
            ? Task.FromResult(response)
            : throw new InvalidOperationException("Keine weiteren Antworten in der Warteschlange.");
    }

    /// <summary>Baut eine 429-Antwort mit Retry-After: 0 (kein Warten im Test).</summary>
    public static HttpResponseMessage TooManyRequests(string body = "{}") =>
        TooManyRequestsWithRetryAfter(TimeSpan.Zero, body);

    public static HttpResponseMessage TooManyRequestsWithRetryAfter(TimeSpan delay, string body = "{}")
    {
        var msg = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        msg.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delay);
        return msg;
    }

    /// <summary>Baut eine einfache 200-OK-Antwort.</summary>
    public static HttpResponseMessage Ok(string body, string contentType = "application/json") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, contentType)
        };
}
