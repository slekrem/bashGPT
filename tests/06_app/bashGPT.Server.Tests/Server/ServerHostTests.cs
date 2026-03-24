using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using bashGPT.Core.Storage;
using bashGPT.Core.Models.Providers;
using bashGPT.Server.Models;
using bashGPT.Server.Services;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für WebApplicationHost: starten den echten Kestrel-Server und
/// prüfen alle API-Endpunkte ohne echte LLM-Verbindung.
/// </summary>
public sealed class ServerHostTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private HttpClient _client = null!;
    private WebApplication _app = null!;
    private string _tempDir = string.Empty;

    // ── Setup / Teardown ────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-server-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var sessionStore = new SessionStore(Path.Combine(_tempDir, "sessions"));

        var webRoot = Path.Combine(_tempDir, "wwwroot");
        Directory.CreateDirectory(webRoot);
        await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"),
            "<!DOCTYPE html><html><body>bashGPT</body></html>");
        await File.WriteAllTextAsync(Path.Combine(webRoot, "bundle.js"),
            "// bashGPT bundle");

        (_app, _client) = await TestServerFactory.CreateAsync(_handler, sessionStore: sessionStore,
            webRootPath: webRoot);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ── GET / ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Root_Returns200WithHtml()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    // ── GET /bundle.js ──────────────────────────────────────────────────────

    [Fact]
    public async Task Get_BundleJs_Returns200WithJavaScript()
    {
        var response = await _client.GetAsync("/bundle.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_Version_ReturnsVersionMetadata()
    {
        var response = await _client.GetAsync("/api/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("bashGPT.Server", json.GetProperty("application").GetString());
        Assert.Equal("0.1.0.0", json.GetProperty("version").GetString());
        Assert.StartsWith("0.1.0", json.GetProperty("informationalVersion").GetString(), StringComparison.Ordinal);
        Assert.Equal("https://github.com/slekrem/bashGPT", json.GetProperty("repositoryUrl").GetString());
    }

    // ── GET /api/history ────────────────────────────────────────────────────

    [Fact]
    public async Task Get_History_InitiallyEmpty()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/history");

        var history = json.GetProperty("history");
        Assert.Equal(JsonValueKind.Array, history.ValueKind);
        Assert.Equal(0, history.GetArrayLength());
    }

    [Fact]
    public async Task Get_History_AfterChat_ContainsMessages()
    {
        await PostChatAsync("Hallo Welt");

        var json = await _client.GetFromJsonAsync<JsonElement>("/api/history");
        var history = json.GetProperty("history");

        Assert.Equal(2, history.GetArrayLength()); // user + assistant
        Assert.Equal("user", history[0].GetProperty("role").GetString());
        Assert.Equal("assistant", history[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task Get_History_AfterToolCallChat_ContainsToolMessages()
    {
        _handler.NextResult = new ServerChatResult(
            Response: "Final answer",
            Logs: [],
            UsedToolCalls: true,
            ConversationDelta:
            [
                ChatMessage.AssistantWithToolCalls(
                    [new ToolCall("call-1", "fetch", """{"url":"https://example.com"}""")],
                    content: ""),
                ChatMessage.ToolResult("Fetched content", "call-1", "fetch"),
                new ChatMessage(ChatRole.Assistant, "Final answer")
            ]);

        await PostChatAsync("Summarize example.com");

        var json = await _client.GetFromJsonAsync<JsonElement>("/api/history");
        var history = json.GetProperty("history");

        Assert.Equal(4, history.GetArrayLength()); // user + assistant(tool_calls) + tool + assistant(final)
        Assert.Equal("user", history[0].GetProperty("role").GetString());
        Assert.Equal("assistant", history[1].GetProperty("role").GetString());
        Assert.Equal("tool", history[2].GetProperty("role").GetString());
        Assert.Equal("assistant", history[3].GetProperty("role").GetString());
        Assert.Equal("Fetched content", history[2].GetProperty("content").GetString());
        Assert.Equal("Final answer", history[3].GetProperty("content").GetString());
    }

    // ── POST /api/reset ─────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Reset_ClearsHistory()
    {
        await PostChatAsync("eine Nachricht");
        var before = await _client.GetFromJsonAsync<JsonElement>("/api/history");
        Assert.NotEqual(0, before.GetProperty("history").GetArrayLength());

        var resetResponse = await _client.PostAsync("/api/reset", null);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var after = await _client.GetFromJsonAsync<JsonElement>("/api/history");
        Assert.Equal(0, after.GetProperty("history").GetArrayLength());
    }

    // ── POST /api/chat ───────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Chat_ValidPrompt_Returns200WithResponse()
    {
        _handler.NextResult = new("Das ist eine Test-Antwort.", []);

        var response = await PostChatAsync("Was ist Zeit?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Das ist eine Test-Antwort.", json.GetProperty("response").GetString());
    }

    [Fact]
    public async Task Post_Chat_EmptyPrompt_Returns400()
    {
        var response = await SendChatRaw(prompt: "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Post_Chat_MissingPrompt_Returns400()
    {
        var body = JsonSerializer.Serialize(new { verbose = false });
        var response = await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Chat_HandlerThrows_Returns500()
    {
        _handler.NextException = new InvalidOperationException("Simulierter Fehler");

        var response = await PostChatAsync("Irgendwas");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // 500 responses now use RFC 7807 Problem Details format
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Internal server error.", json.GetProperty("detail").GetString());
        Assert.DoesNotContain("Simulierter Fehler", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_ChatCancel_UnknownRequest_ReturnsCancelledFalse()
    {
        var body = JsonSerializer.Serialize(new { requestId = "missing-request" });
        var response = await _client.PostAsync("/api/chat/cancel", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("ok").GetBoolean());
        Assert.False(payload.GetProperty("cancelled").GetBoolean());
    }

    [Fact]
    public async Task Post_ChatCancel_CancelsStreamingRequest_WithUserCancelledStatus()
    {
        _handler.WaitForCancellation = true;
        var requestId = $"req-{Guid.NewGuid():N}";

        var streamBody = JsonSerializer.Serialize(new { prompt = "Bitte warten", requestId });
        var streamRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = new StringContent(streamBody, Encoding.UTF8, "application/json"),
        };

        var streamResponseTask = _client.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead);
        await Task.Delay(150);

        var cancelBody = JsonSerializer.Serialize(new { requestId });
        var cancelResponse = await _client.PostAsync("/api/chat/cancel", new StringContent(cancelBody, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelPayload = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cancelPayload.GetProperty("cancelled").GetBoolean());

        var streamResponse = await streamResponseTask;
        var ssePayload = await streamResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"finalStatus\":\"user_cancelled\"", ssePayload);
        Assert.Contains("[DONE]", ssePayload);
    }

    [Fact]
    public async Task Post_ChatStream_HandlerThrows_ReturnsSanitizedErrorEvent()
    {
        _handler.NextException = new InvalidOperationException("Vertrauliches Streaming-Detail");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { prompt = "Bitte streamen" }),
                Encoding.UTF8,
                "application/json"),
        };

        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var ssePayload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Internal server error.", ssePayload, StringComparison.Ordinal);
        Assert.DoesNotContain("Vertrauliches Streaming-Detail", ssePayload, StringComparison.Ordinal);
        Assert.Contains("[DONE]", ssePayload, StringComparison.Ordinal);
    }

    // ── 404 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/not-existing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> PostChatAsync(string prompt) => await SendChatRaw(prompt);

    private async Task<HttpResponseMessage> SendChatRaw(string? prompt = null)
    {
        var payload = new Dictionary<string, object?>();
        if (prompt is not null) payload["prompt"] = prompt;

        var body = JsonSerializer.Serialize(payload);
        return await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }
}
