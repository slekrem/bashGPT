using System.Net;
using System.Text;
using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Fetch;

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
        var output = JsonSerializer.Deserialize<FetchOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(200, output.StatusCode);
        Assert.Equal("hello world", output.Body);
        Assert.False(output.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_Get404_ReturnsFailure()
    {
        var tool = CreateTool(new FakeHandler(HttpStatusCode.NotFound, "not found"));

        var result = await tool.ExecuteAsync(Call("https://example.com/missing"), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<FetchOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(404, output.StatusCode);
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
        Assert.Contains("Invalid arguments", result.Content);
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
        var output = JsonSerializer.Deserialize<FetchOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.True(output.TimedOut);
    }

    [Fact]
    public void DefaultFetchPolicy_AllowsHttpAndHttps()
    {
        var policy = new DefaultFetchPolicy();

        Assert.True(policy.Allow(new FetchInput("https://example.com", "GET", null, null, 5000)));
        Assert.True(policy.Allow(new FetchInput("http://example.com", "POST", null, null, 5000)));
    }

    [Fact]
    public void DefaultFetchPolicy_BlocksNonHttpSchemes()
    {
        var policy = new DefaultFetchPolicy();

        Assert.False(policy.Allow(new FetchInput("file:///etc/passwd", "GET", null, null, 5000)));
        Assert.False(policy.Allow(new FetchInput("ftp://example.com", "GET", null, null, 5000)));
    }

    private sealed class FakeHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain")
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
