using System.Net;
using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Tests.Providers;

public class CerebrasProviderTests
{
    private static CerebrasProvider CreateProvider(
        string responseBody,
        HttpStatusCode status = HttpStatusCode.OK,
        string? apiKey = "test-key")
    {
        var handler = new TestHttpMessageHandler(responseBody, status, "text/event-stream");
        var http    = new HttpClient(handler);
        var config  = new CerebrasConfig
        {
            BaseUrl = "https://api.cerebras.ai/v1",
            Model   = "gpt-oss:120b-cloud",
            ApiKey  = apiKey
        };
        return new CerebrasProvider(config, http);
    }

    [Fact]
    public async Task StreamAsync_YieldsTokensFromSse()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"Hallo"}}]}
            data: {"choices":[{"delta":{"content":" Welt"}}]}
            data: [DONE]
            """;

        var provider = CreateProvider(sse);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["Hallo", " Welt"], tokens);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsConcatenatedTokens()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"foo"}}]}
            data: {"choices":[{"delta":{"content":"bar"}}]}
            data: [DONE]
            """;

        var provider = CreateProvider(sse);
        var result = await provider.CompleteAsync([new ChatMessage(ChatRole.User, "test")]);

        Assert.Equal("foobar", result);
    }

    [Fact]
    public async Task StreamAsync_StopsAtDone()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"A"}}]}
            data: [DONE]
            data: {"choices":[{"delta":{"content":"B"}}]}
            """;

        var provider = CreateProvider(sse);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["A"], tokens);
    }

    [Fact]
    public async Task StreamAsync_IgnoresNonDataLines()
    {
        var sse = """
            : keep-alive
            data: {"choices":[{"delta":{"content":"ok"}}]}
            data: [DONE]
            """;

        var provider = CreateProvider(sse);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["ok"], tokens);
    }

    [Fact]
    public async Task StreamAsync_Throws_WhenNoApiKey()
    {
        var provider = CreateProvider("", apiKey: null);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")])) { }
        });
    }

    [Fact]
    public async Task StreamAsync_Throws_On401_WithHint()
    {
        var provider = CreateProvider("{}", HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")])) { }
        });

        Assert.Contains("ungültig", ex.Message);
    }

    [Fact]
    public async Task StreamAsync_Throws_On429_WithHint()
    {
        var provider = CreateProvider("{}", HttpStatusCode.TooManyRequests);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")])) { }
        });

        Assert.Contains("Rate-Limit", ex.Message);
    }

    [Fact]
    public void Name_And_Model_AreCorrect()
    {
        var provider = CreateProvider("");
        Assert.Equal("Cerebras",          provider.Name);
        Assert.Equal("gpt-oss:120b-cloud", provider.Model);
    }

    [Fact]
    public async Task StreamAsync_SendsAuthorizationHeader()
    {
        var handler = new TestHttpMessageHandler("data: [DONE]", contentType: "text/event-stream");
        var http    = new HttpClient(handler);
        var config  = new CerebrasConfig { ApiKey = "my-secret-key", Model = "gpt-oss:120b-cloud" };
        var provider = new CerebrasProvider(config, http);

        await foreach (var _ in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")])) { }

        Assert.Equal("Bearer my-secret-key",
            handler.LastRequest?.Headers.Authorization?.ToString());
    }
}
