using System.Net;

namespace BashGPT.Tests.Providers;

/// <summary>
/// Einfacher Mock-Handler für HttpClient-Tests.
/// </summary>
internal sealed class TestHttpMessageHandler(
    string responseBody,
    HttpStatusCode statusCode = HttpStatusCode.OK,
    string contentType = "application/x-ndjson") : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody,
                System.Text.Encoding.UTF8, contentType)
        };
        return Task.FromResult(response);
    }
}
