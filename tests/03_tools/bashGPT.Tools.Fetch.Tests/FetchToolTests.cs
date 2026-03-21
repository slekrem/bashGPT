using System.Net;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Fetch;

namespace bashGPT.Tools.Fetch.Tests;

public class FetchToolTests
{
    private static ToolCall Call(string url, string? method = null, string? body = null, int? timeoutMs = null)
    {
        var args = new Dictionary<string, object?> { ["url"] = url };
        if (method is not null) args["method"] = method;
        if (body is not null) args["body"] = body;
        if (timeoutMs is not null) args["timeoutMs"] = timeoutMs;
        return new ToolCall("fetch", JsonSerializer.Serialize(args));
    }

    private static FetchTool CreateTool(HttpMessageHandler handler, Action<FetchInput, FetchOutput>? onExecuted = null)
        => new(httpClient: new HttpClient(handler), onExecuted: onExecuted);

    [Fact]
    public async Task ExecuteAsync_Get200_ReturnsSuccess()
    {
        var tool = CreateTool(new FakeHandler(HttpStatusCode.OK, "hello world"));

        var result = await tool.ExecuteAsync(Call("https://example.com"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("hello world", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_Get404_ReturnsFailure()
    {
        var tool = CreateTool(new FakeHandler(HttpStatusCode.NotFound, "not found"));

        var result = await tool.ExecuteAsync(Call("https://example.com/missing"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("not found", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_PolicyBlocked_ReturnsFailure()
    {
        var tool = new FetchTool(policy: new BlockAllPolicy());

        var result = await tool.ExecuteAsync(Call("https://example.com"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidUrl_BlockedByDefaultPolicy()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(Call("file:///etc/passwd"), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(new ToolCall("fetch", "{not-valid-json}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUrl_ReturnsStructuredValidationError()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(new ToolCall("fetch", """{"method":"GET"}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'url'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyUrl_ReturnsStructuredValidationError()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(new ToolCall("fetch", """{"url":""}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'url'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidHeadersType_ReturnsStructuredValidationError()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(
            new ToolCall("fetch", """{"url":"https://example.com","headers":"x"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'headers'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimeoutType_ReturnsStructuredValidationError()
    {
        var tool = new FetchTool();

        var result = await tool.ExecuteAsync(
            new ToolCall("fetch", """{"url":"https://example.com","timeoutMs":"fast"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'timeoutMs'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_LoggingHook_IsCalled()
    {
        FetchInput? capturedInput = null;
        FetchOutput? capturedOutput = null;

        var tool = CreateTool(
            new FakeHandler(HttpStatusCode.OK, "hook-test"),
            onExecuted: (i, o) => { capturedInput = i; capturedOutput = o; });

        await tool.ExecuteAsync(Call("https://example.com"), CancellationToken.None);

        Assert.NotNull(capturedInput);
        Assert.NotNull(capturedOutput);
        Assert.Equal("https://example.com", capturedInput!.Url);
        Assert.Contains("hook-test", capturedOutput!.Body);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_SetsTimedOutTrue()
    {
        var tool = CreateTool(new DelayHandler(TimeSpan.FromSeconds(10)));

        var result = await tool.ExecuteAsync(Call("https://example.com", timeoutMs: 100), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("timed out", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_HtmlResponse_ExtractsStructuredText()
    {
        const string html = """
            <html>
              <head><title>Example Article</title></head>
              <body>
                <nav>ignore me</nav>
                <h1>Main Heading</h1>
                <p>First paragraph.</p>
                <p>Second paragraph.</p>
                <a href="https://example.com/more">Read more</a>
              </body>
            </html>
            """;
        var tool = CreateTool(new FakeHandler(HttpStatusCode.OK, html, "text/html"));

        var result = await tool.ExecuteAsync(Call("https://example.com/article"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Title: Example Article", result.Content);
        Assert.Contains("# Main Heading", result.Content);
        Assert.Contains("First paragraph.", result.Content);
        Assert.Contains("Links:", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_NonHtmlResponse_DoesNotPopulateHtmlFields()
    {
        var tool = CreateTool(new FakeHandler(HttpStatusCode.OK, "plain text", "text/plain"));

        var result = await tool.ExecuteAsync(Call("https://example.com/plain"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("plain text", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_BrokenHtml_DoesNotFailRequest()
    {
        const string brokenHtml = "<html><head><title>Broken<title></head><body><h1>Heading<p>Text";
        var tool = CreateTool(new FakeHandler(HttpStatusCode.OK, brokenHtml, "text/html"));

        var result = await tool.ExecuteAsync(Call("https://example.com/broken"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Heading", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_LargeResponse_DefaultLimit_ReturnsCompleteBody()
    {
        var content = new string('x', 40_000);
        var tool = CreateTool(new FakeHandler(HttpStatusCode.OK, content));

        var result = await tool.ExecuteAsync(Call("https://example.com/large"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(40_000, result.Content.Length);
    }

    [Fact]
    public void DefaultFetchPolicy_AllowsReadOnlyHttpAndHttps()
    {
        var policy = new DefaultFetchPolicy();

        Assert.True(policy.Allow(new FetchInput("https://example.com", "GET", null, null, 5000)));
        Assert.True(policy.Allow(new FetchInput("http://example.com", "HEAD", null, null, 5000)));
    }

    [Fact]
    public void DefaultFetchPolicy_BlocksMutatingMethods()
    {
        var policy = new DefaultFetchPolicy();

        Assert.False(policy.Allow(new FetchInput("https://example.com", "POST", null, null, 5000)));
        Assert.False(policy.Allow(new FetchInput("https://example.com", "DELETE", null, null, 5000)));
    }

    [Fact]
    public void DefaultFetchPolicy_BlocksNonHttpSchemes()
    {
        var policy = new DefaultFetchPolicy();

        Assert.False(policy.Allow(new FetchInput("file:///etc/passwd", "GET", null, null, 5000)));
        Assert.False(policy.Allow(new FetchInput("ftp://example.com", "GET", null, null, 5000)));
    }

    private sealed class FakeHandler(HttpStatusCode statusCode, string body, string contentType = "text/plain") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType)
            });
    }

    private sealed class DelayHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class BlockAllPolicy : IFetchPolicy
    {
        public bool Allow(FetchInput input) => false;
    }
}
