using System.Net;

namespace bashGPT.Core.Tests.Providers;

/// <summary>
/// Returns the same response for every request.
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

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, contentType)
        };
    }
}

/// <summary>
/// Returns a queued sequence of <see cref="HttpResponseMessage"/> instances.
/// Useful for retry tests, for example 429 first and 200 second.
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
            : throw new InvalidOperationException("No more responses are queued.");
    }

    /// <summary>
    /// Builds a 429 response with Retry-After: 0 to avoid delays in tests.
    /// </summary>
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

    /// <summary>
    /// Builds a simple 200 OK response.
    /// </summary>
    public static HttpResponseMessage Ok(string body, string contentType = "application/json") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, contentType)
        };
}
