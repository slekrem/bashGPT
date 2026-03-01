using System.Net;
using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Tests.Providers;

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
        // Ollama ndjson-Stream: zwei Chunks + done
        var ndjson = """
            {"message":{"role":"assistant","content":"Hallo"},"done":false}
            {"message":{"role":"assistant","content":" Welt"},"done":false}
            {"message":{"role":"assistant","content":""},"done":true}
            """;

        var provider = CreateProvider(ndjson);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["Hallo", " Welt"], tokens);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsConcatenatedTokens()
    {
        var ndjson = """
            {"message":{"role":"assistant","content":"foo"},"done":false}
            {"message":{"role":"assistant","content":"bar"},"done":true}
            """;

        var provider = CreateProvider(ndjson);
        var result = await provider.CompleteAsync([new ChatMessage(ChatRole.User, "test")]);

        Assert.Equal("foobar", result);
    }

    [Fact]
    public async Task StreamAsync_StopsAtDone()
    {
        var ndjson = """
            {"message":{"role":"assistant","content":"A"},"done":false}
            {"message":{"role":"assistant","content":""},"done":true}
            {"message":{"role":"assistant","content":"B"},"done":false}
            """;

        var provider = CreateProvider(ndjson);
        var tokens = new List<string>();

        await foreach (var t in provider.StreamAsync([new ChatMessage(ChatRole.User, "test")]))
            tokens.Add(t);

        Assert.Equal(["A"], tokens);
    }

    [Fact]
    public async Task StreamAsync_SkipsInvalidJsonLines()
    {
        var ndjson = """
            {"message":{"role":"assistant","content":"ok"},"done":false}
            not-json-at-all
            {"message":{"role":"assistant","content":""},"done":true}
            """;

        var provider = CreateProvider(ndjson);
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
        var json = """{"message":{"role":"assistant","content":"Hallo Welt","tool_calls":null},"done":true}""";
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
            {"message":{"role":"assistant","content":"",
             "tool_calls":[{"type":"function","function":{"name":"bash","arguments":{"command":"ls -la"}}}]},
             "done":true}
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
        var ndjson = """
            {"message":{"role":"assistant","content":"foo"},"done":false}
            {"message":{"role":"assistant","content":"bar"},"done":true}
            """;
        var tokens = new List<string>();
        var provider = CreateProvider(ndjson);

        var result = await provider.ChatAsync(
            new LlmChatRequest([new ChatMessage(ChatRole.User, "test")],
                Stream: true, OnToken: t => tokens.Add(t)));

        Assert.Equal(["foo", "bar"], tokens);
        Assert.Equal("foobar", result.Content);
    }

    [Fact]
    public async Task ChatAsync_ThrowsLlmProviderException_OnHttpError()
    {
        var provider = CreateProvider("Internal Server Error", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
            await provider.ChatAsync(
                new LlmChatRequest([new ChatMessage(ChatRole.User, "test")], Stream: false)));
    }
}
