using System.Net;
using System.Text.Json;
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

    // ── ChatAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChatAsync_NonStream_ReturnsTextContent()
    {
        var json = """{"choices":[{"message":{"content":"Hallo Welt","tool_calls":null}}]}""";
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new CerebrasProvider(
            new CerebrasConfig { ApiKey = "key", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.Equal("Hallo Welt", result.Content);
        Assert.Empty(result.ToolCalls);
    }

    [Fact]
    public async Task ChatAsync_NonStream_ReturnsToolCall()
    {
        var json = """
            {"choices":[{"message":{"content":null,"tool_calls":[
              {"id":"call_1","type":"function","function":{"name":"bash","arguments":"{\"command\":\"ls -la\"}"}}
            ]}}]}
            """;
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new CerebrasProvider(
            new CerebrasConfig { ApiKey = "key", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "liste dateien")],
                Tools: [ToolDefinitions.Bash], Stream: false));

        Assert.Single(result.ToolCalls);
        Assert.Equal("bash", result.ToolCalls[0].Name);
        Assert.Contains("ls -la", result.ToolCalls[0].ArgumentsJson);
    }

    [Fact]
    public async Task ChatAsync_Stream_ReturnsTextContent()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"foo"}}]}
            data: {"choices":[{"delta":{"content":"bar"}}]}
            data: [DONE]
            """;
        var tokens = new List<string>();
        var provider = CreateProvider(sse);

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")],
                Stream: true, OnToken: t => tokens.Add(t)));

        Assert.Equal(["foo", "bar"], tokens);
        Assert.Equal("foobar", result.Content);
    }

    [Fact]
    public async Task ChatAsync_IncludesConfiguredCerebrasOptions()
    {
        var json = """{"choices":[{"message":{"content":"ok","tool_calls":null}}]}""";
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new CerebrasProvider(
            new CerebrasConfig
            {
                ApiKey = "key",
                Model = "test",
                Temperature = 0.25,
                TopP = 0.9,
                MaxCompletionTokens = 2048,
                Seed = 123,
                ReasoningEffort = "medium"
            },
            new HttpClient(handler));

        _ = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal(0.25, root.GetProperty("temperature").GetDouble());
        Assert.Equal(0.9, root.GetProperty("top_p").GetDouble());
        Assert.Equal(2048, root.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal(123, root.GetProperty("seed").GetInt32());
        Assert.Equal("medium", root.GetProperty("reasoning_effort").GetString());
    }

    // ── 429 Retry-Logik ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChatAsync_Retries_On429_ThenSucceeds()
    {
        // Retry-After: 0 → kein Warten im Test
        var successBody = """{"choices":[{"message":{"content":"OK","tool_calls":null}}]}""";
        var handler = new SequentialHttpMessageHandler(
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.Ok(successBody));

        var provider = new CerebrasProvider(
            new CerebrasConfig { ApiKey = "key", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.Equal(2, handler.CallCount);  // einmal 429, einmal 200
        Assert.Equal("OK", result.Content);
    }

    [Fact]
    public async Task ChatAsync_RetriesTwice_ThenSucceeds()
    {
        var successBody = """{"choices":[{"message":{"content":"Erfolg","tool_calls":null}}]}""";
        var handler = new SequentialHttpMessageHandler(
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.Ok(successBody));

        var provider = new CerebrasProvider(
            new CerebrasConfig { ApiKey = "key", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.Equal(3, handler.CallCount);
        Assert.Equal("Erfolg", result.Content);
    }

    [Fact]
    public async Task ChatAsync_ThrowsAfterMaxRetries()
    {
        // 4 × 429 überschreitet das Limit von 3 Retries (= 4 Gesamtversuche)
        var handler = new SequentialHttpMessageHandler(
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.TooManyRequests(),
            SequentialHttpMessageHandler.TooManyRequests());

        var provider = new CerebrasProvider(
            new CerebrasConfig { ApiKey = "key", Model = "test" },
            new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<LlmProviderException>(async () =>
            await provider.ChatAsync(
                new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false)));

        Assert.Equal(4, handler.CallCount);
        Assert.Contains("Rate-Limit", ex.Message);
    }
}
