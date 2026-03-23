using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using bashGPT.Server;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für die /api/settings-Endpunkte des WebApplicationHost.
/// </summary>
public sealed class ServerHostSettingsTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private HttpClient _client = null!;
    private WebApplication _app = null!;
    private string _configFile = string.Empty;
    private TestConfigurationService _configService = null!;

    public async Task InitializeAsync()
    {
        _configFile = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}.json");
        _configService = new TestConfigurationService(_configFile);

        (_app, _client) = await TestServerFactory.CreateAsync(_handler, configService: _configService);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();

        if (File.Exists(_configFile))
            File.Delete(_configFile);
    }

    // ── GET /api/settings ohne ConfigService ────────────────────────────────

    [Fact]
    public async Task Get_Settings_WithoutConfigService_Returns503()
    {
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.GetAsync("/api/settings");

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── GET /api/settings ───────────────────────────────────────────────────

    [Fact]
    public async Task Get_Settings_Returns200_WithDefaultValues()
    {
        var response = await _client.GetAsync("/api/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("provider", out var provider));
        Assert.True(json.TryGetProperty("model", out _));
        Assert.True(json.TryGetProperty("ollamaHost", out _));
        Assert.False(json.TryGetProperty("forceTools", out _));

        Assert.Equal("ollama", provider.GetString());
    }

    [Fact]
    public async Task Get_Settings_DoesNotExposeRemovedProviderFields()
    {
        var response = await _client.GetAsync("/api/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("hasApiKey", out _));
        Assert.False(json.TryGetProperty("apiKey", out _));
        Assert.False(json.TryGetProperty("providerConfig", out _));
    }

    [Fact]
    public async Task Unknown_Settings_Route_Returns404()
    {
        var response = await _client.GetAsync("/api/settings/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/settings ───────────────────────────────────────────────────

    [Fact]
    public async Task Put_Settings_UpdatesProvider_And_Persists()
    {
        var body = JsonSerializer.Serialize(new { provider = "ollama", model = "test-model" });
        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal("test-model", config.Ollama.Model);
    }

    [Fact]
    public async Task Put_Settings_WithNullBody_Returns400()
    {
        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }

    [Fact]
    public async Task Put_Settings_NestedProviderPayload_UpdatesOllama()
    {
        var body = JsonSerializer.Serialize(new
        {
            provider = "ollama",
            ollama = new { model = "ollama-model-y", host = "http://ollama.local:11434" },
        });

        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal("ollama-model-y", config.Ollama.Model);
        Assert.Equal("http://ollama.local:11434", config.Ollama.BaseUrl);
    }

    [Fact]
    public async Task Put_Settings_LegacyApiKeyField_IsIgnored()
    {
        var body = JsonSerializer.Serialize(new { apiKey = "legacy-key", model = "ollama-updated" });
        var response = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal("ollama-updated", config.Ollama.Model);
    }

    // ── POST /api/settings/test ─────────────────────────────────────────────

    [Fact]
    public async Task Post_SettingsTest_Returns200_WithOkOrError()
    {
        var response = await _client.PostAsync("/api/settings/test", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("ok", out var ok));

        if (ok.GetBoolean())
            Assert.True(json.TryGetProperty("latencyMs", out _));
        else
            Assert.True(json.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Post_SettingsTest_Failure_ReturnsSanitizedError()
    {
        var config = await _configService.LoadAsync();
        config.Ollama.BaseUrl = "http://127.0.0.1:1";
        await _configService.SaveAsync(config);

        var response = await _client.PostAsync("/api/settings/test", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(raw);
        Assert.False(json.GetProperty("ok").GetBoolean());
        Assert.Equal("Connection test failed.", json.GetProperty("error").GetString());
        Assert.DoesNotContain("127.0.0.1:1", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Put_Settings_WithoutConfigService_Returns503()
    {
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.PutAsync("/api/settings",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Post_SettingsTest_WithoutConfigService_Returns503()
    {
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.PostAsync("/api/settings/test", null);

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
