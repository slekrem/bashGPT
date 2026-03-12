using System.Net;
using System.Text.Json;
using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Core.Tests.Providers;

public class OllamaProviderTests
{
    private static OllamaProvider CreateProvider(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new TestHttpMessageHandler(responseBody, status);
        var http    = new HttpClient(handler);
        var config  = new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "gpt-oss:20b" };
        return new OllamaProvider(config, http);
    }

    [Fact]
    public async Task StreamAsync_YieldsTokens()
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
    public async Task StreamAsync_SkipsInvalidJsonLines()
    {
        var sse = """
            data: {"choices":[{"delta":{"content":"ok"}}]}

            data: not-json-at-all

            data: [DONE]
            """;

        var provider = CreateProvider(sse);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["ok"], tokens);
    }

    [Fact]
    public async Task StreamAsync_ThrowsLlmProviderException_OnHttpError()
    {
        var provider = CreateProvider("Internal Server Error", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")])) { }
        });
    }

    [Fact]
    public void Name_And_Model_AreCorrect()
    {
        var provider = CreateProvider("");
        Assert.Equal("Ollama",      provider.Name);
        Assert.Equal("gpt-oss:20b", provider.Model);
    }

    // ── ChatAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChatAsync_NonStream_ReturnsTextContent()
    {
        var json = """{"choices":[{"message":{"role":"assistant","content":"Hallo Welt"}}],"usage":{"prompt_tokens":5,"completion_tokens":2}}""";
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
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
            {"choices":[{"message":{"role":"assistant","content":"",
             "tool_calls":[{"id":"tc1","type":"function","function":{"name":"bash","arguments":"{\"command\":\"ls -la\"}"}}]}}],
             "usage":{"prompt_tokens":5,"completion_tokens":10}}
            """;
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
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
    public async Task ChatAsync_Stream_IncompleteStream_RetriesMandatory_AndSucceeds()
    {
        var firstIncomplete = """
            data: {"choices":[{"delta":{"reasoning":"plan"}}]}
            """;
        var secondComplete = """
            data: {"choices":[{"delta":{"content":"ok"}}]}
            data: [DONE]
            """;
        var handler = new SequentialHttpMessageHandler(
            SequentialHttpMessageHandler.Ok(firstIncomplete, "text/event-stream"),
            SequentialHttpMessageHandler.Ok(secondComplete, "text/event-stream"));
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: true));

        Assert.Equal(2, handler.CallCount);
        Assert.Equal("ok", result.Content);
    }

    [Fact]
    public async Task ChatAsync_Stream_IncompleteStream_StillEmitsResponseJson_OnFailure()
    {
        var incomplete = """
            data: {"choices":[{"delta":{"reasoning":"plan"}}]}
            """;
        var handler = new SequentialHttpMessageHandler(
            SequentialHttpMessageHandler.Ok(incomplete, "text/event-stream"),
            SequentialHttpMessageHandler.Ok(incomplete, "text/event-stream"),
            SequentialHttpMessageHandler.Ok(incomplete, "text/event-stream"),
            SequentialHttpMessageHandler.Ok(incomplete, "text/event-stream"));
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
            new HttpClient(handler));

        string? capturedResponseJson = null;

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
            await provider.ChatAsync(new LlmChatRequest(
                [new ChatMessage(ChatRole.User, "test")],
                Stream: true,
                OnResponseJson: json =>
                {
                    capturedResponseJson = json;
                    return Task.CompletedTask;
                })));

        Assert.NotNull(capturedResponseJson);
        Assert.Contains("# attempt 1", capturedResponseJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChatAsync_ThrowsLlmProviderException_OnHttpError()
    {
        var provider = CreateProvider("Internal Server Error", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
            await provider.ChatAsync(
                new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false)));
    }

    [Fact]
    public async Task ChatAsync_RecoverToolCall_WhenReasoningModelPrefixesJson()
    {
        // Ollama HTTP 500, weil ein Reasoning-Modell Denktext vor das JSON geschrieben hat
        var errorBody = """
            {"error":{"message":"error parsing tool call: raw='We need to list files. {\"command\":\"ls -la\"}', err=invalid character 'W' looking for beginning of value","type":"api_error","param":null,"code":null}}
            """;
        var handler = new TestHttpMessageHandler(errorBody, HttpStatusCode.InternalServerError, contentType: "application/json");
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
            new HttpClient(handler));

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "liste dateien")],
                Tools: [ToolDefinitions.Bash], Stream: false));

        Assert.Single(result.ToolCalls);
        Assert.Equal("bash", result.ToolCalls[0].Name);
        Assert.Contains("ls -la", result.ToolCalls[0].ArgumentsJson);
    }

    [Fact]
    public async Task ChatAsync_StillThrows_WhenHttp500IsNotToolCallError()
    {
        var provider = CreateProvider("Internal Server Error", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
            await provider.ChatAsync(
                new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false)));
    }

    [Fact]
    public async Task ChatAsync_SendsOpenAiCompatibleRequest()
    {
        var json = """{"choices":[{"message":{"role":"assistant","content":"ok"}}],"usage":{"prompt_tokens":5,"completion_tokens":2}}""";
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new OllamaProvider(
            new OllamaConfig
            {
                BaseUrl = "http://localhost:11434",
                Model = "test",
                Temperature = 0.2,
                TopP = 0.95,
                Seed = 42,
            },
            new HttpClient(handler));

        _ = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("test", root.GetProperty("model").GetString());
        Assert.Equal(0.2, root.GetProperty("temperature").GetDouble());
        Assert.Equal(0.95, root.GetProperty("top_p").GetDouble());
        Assert.Equal(42, root.GetProperty("seed").GetInt32());
        Assert.True(root.TryGetProperty("options", out var options));
        Assert.Equal(65536, options.GetProperty("num_ctx").GetInt32());
    }

    [Fact]
    public async Task ChatAsync_UsesCorrectEndpoint()
    {
        var json = """{"choices":[{"message":{"role":"assistant","content":"ok"}}],"usage":{"prompt_tokens":1,"completion_tokens":1}}""";
        var handler = new TestHttpMessageHandler(json, contentType: "application/json");
        var provider = new OllamaProvider(
            new OllamaConfig { BaseUrl = "http://localhost:11434", Model = "test" },
            new HttpClient(handler));

        _ = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false));

        Assert.NotNull(handler.LastRequest?.RequestUri);
        Assert.Equal("/v1/chat/completions", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
