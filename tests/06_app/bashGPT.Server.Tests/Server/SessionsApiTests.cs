using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using bashGPT.Core.Storage;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration tests for the /api/sessions endpoints (SessionsController).
/// </summary>
public sealed class SessionsApiTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private HttpClient _client = null!;
    private WebApplication _app = null!;
    private SessionStore _sessionStore = null!;
    private string _tempDir = string.Empty;

    public async Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-sessions-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _sessionStore = new SessionStore(Path.Combine(_tempDir, "sessions"));

        (_app, _client) = await TestServerFactory.CreateAsync(_handler, sessionStore: _sessionStore);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ── GET /api/sessions ───────────────────────────────────────────────────

    [Fact]
    public async Task Get_Sessions_InitiallyEmpty()
    {
        var response = await _client.GetAsync("/api/sessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.GetProperty("sessions").ValueKind);
        Assert.Equal(0, json.GetProperty("sessions").GetArrayLength());
    }

    [Fact]
    public async Task Get_Sessions_WithoutStore_Returns503()
    {
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.GetAsync("/api/sessions");

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── POST /api/sessions ──────────────────────────────────────────────────

    [Fact]
    public async Task Post_Sessions_CreatesNewSession()
    {
        var response = await _client.PostAsync("/api/sessions", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("id", out var id));
        Assert.StartsWith("s-", id.GetString(), StringComparison.Ordinal);
        Assert.Equal("New Chat", json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Post_Sessions_AppearInSessionList()
    {
        await _client.PostAsync("/api/sessions", null);
        await _client.PostAsync("/api/sessions", null);

        var response = await _client.GetAsync("/api/sessions");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, json.GetProperty("sessions").GetArrayLength());
    }

    [Fact]
    public async Task Post_Sessions_WithoutStore_Returns503()
    {
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.PostAsync("/api/sessions", null);

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── GET /api/sessions/<id> ──────────────────────────────────────────────

    [Fact]
    public async Task Get_SessionById_ReturnsSession()
    {
        var created = await (await _client.PostAsync("/api/sessions", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var response = await _client.GetAsync($"/api/sessions/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, json.GetProperty("id").GetString());
        Assert.Equal("New Chat", json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_SessionById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/sessions/nonexistent-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out _));
    }

    // ── PUT /api/sessions/<id> ──────────────────────────────────────────────

    [Fact]
    public async Task Put_SessionById_UpdatesTitle()
    {
        var created = await (await _client.PostAsync("/api/sessions", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var body = JsonSerializer.Serialize(new { title = "Updated Title" });
        var putResponse = await _client.PutAsync($"/api/sessions/{id}",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var ok = await putResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(ok.GetProperty("ok").GetBoolean());

        var get = await _client.GetAsync($"/api/sessions/{id}");
        var session = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Title", session.GetProperty("title").GetString());
    }

    // ── DELETE /api/sessions/<id> ───────────────────────────────────────────

    [Fact]
    public async Task Delete_SessionById_RemovesSession()
    {
        var created = await (await _client.PostAsync("/api/sessions", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var deleteResponse = await _client.DeleteAsync($"/api/sessions/{id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var sessions = await _client.GetFromJsonAsync<JsonElement>("/api/sessions");
        Assert.Equal(0, sessions.GetProperty("sessions").GetArrayLength());
    }

    // ── POST /api/sessions/clear ────────────────────────────────────────────

    [Fact]
    public async Task Post_Sessions_Clear_RemovesAllSessions()
    {
        await _client.PostAsync("/api/sessions", null);
        await _client.PostAsync("/api/sessions", null);

        var clearResponse = await _client.PostAsync("/api/sessions/clear", null);
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);
        var ok = await clearResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(ok.GetProperty("ok").GetBoolean());

        var sessions = await _client.GetFromJsonAsync<JsonElement>("/api/sessions");
        Assert.Equal(0, sessions.GetProperty("sessions").GetArrayLength());
    }
}
