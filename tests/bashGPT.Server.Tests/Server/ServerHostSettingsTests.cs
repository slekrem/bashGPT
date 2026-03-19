using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Shell;

namespace BashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für die /api/settings-Endpunkte des ServerHost.
/// </summary>
public sealed class ServerHostSettingsTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private readonly HttpClient _client = new();
    private ServerHost _server = null!;
    private CancellationTokenSource _cts = null!;
    private Task _serverTask = null!;
    private string _baseUrl = string.Empty;
    private string _configFile = string.Empty;
    private TestConfigurationService _configService = null!;

    public async Task InitializeAsync()
    {
        _configFile = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}.json");
        _configService = new TestConfigurationService(_configFile);

        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";
        _client.BaseAddress = new Uri(_baseUrl);

        var options = new ServerOptions(
            Port: port,
            NoBrowser: true,
            Model: null,
            Verbose: false);

        _server = new ServerHost(_handler, _configService);
        _cts = new CancellationTokenSource();
        _serverTask = _server.RunAsync(options, _cts.Token);

        await WaitForServerAsync(_baseUrl);
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            using var probe = new HttpClient();
            await probe.GetAsync($"{_baseUrl}/").ConfigureAwait(false);
        }
        catch { /* ignorieren */ }

        try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch { /* TaskCanceledException oder Timeout erwartet */ }

        _client.Dispose();
        _cts.Dispose();

        if (File.Exists(_configFile))
            File.Delete(_configFile);
    }

    // ── GET /api/settings ohne ConfigService ────────────────────────────────

    [Fact]
    public async Task Get_Settings_WithoutConfigService_Returns503()
    {
        // Eigener Server ohne ConfigService
        var port = GetFreePort();
        var serverNoConfig = new ServerHost(_handler);
        using var cts = new CancellationTokenSource();
        var options = new ServerOptions(port, true, null, false);
        var task = serverNoConfig.RunAsync(options, cts.Token);
        var url = $"http://127.0.0.1:{port}";
        await WaitForServerAsync(url);

        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var response = await client.GetAsync("/api/settings");

        await cts.CancelAsync();
        try
        {
            using var probe = new HttpClient();
            await probe.GetAsync($"{url}/");
        }
        catch { }
        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }

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
        Assert.False(json.TryGetProperty("execMode", out _));
        Assert.False(json.TryGetProperty("forceTools", out _));

        // Standardprovider ist Ollama
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

        // Config-Datei prüfen
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
        // Ollama ist in CI möglicherweise nicht verfügbar → HTTP 200 in jedem Fall
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
        Assert.Equal("Verbindungstest fehlgeschlagen.", json.GetProperty("error").GetString());
        Assert.DoesNotContain("127.0.0.1:1", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Put_Settings_WithoutConfigService_Returns503()
    {
        var port = GetFreePort();
        var serverNoConfig = new ServerHost(_handler);
        using var cts = new CancellationTokenSource();
        var options = new ServerOptions(port, true, null, false);
        var task = serverNoConfig.RunAsync(options, cts.Token);
        var url = $"http://127.0.0.1:{port}";
        await WaitForServerAsync(url);

        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var response = await client.PutAsync("/api/settings",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        await cts.CancelAsync();
        try
        {
            using var probe = new HttpClient();
            await probe.GetAsync($"{url}/");
        }
        catch { }
        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Post_SettingsTest_WithoutConfigService_Returns503()
    {
        var port = GetFreePort();
        var serverNoConfig = new ServerHost(_handler);
        using var cts = new CancellationTokenSource();
        var options = new ServerOptions(port, true, null, false);
        var task = serverNoConfig.RunAsync(options, cts.Token);
        var url = $"http://127.0.0.1:{port}";
        await WaitForServerAsync(url);

        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var response = await client.PostAsync("/api/settings/test", null);

        await cts.CancelAsync();
        try
        {
            using var probe = new HttpClient();
            await probe.GetAsync($"{url}/");
        }
        catch { }
        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForServerAsync(string baseUrl, int maxWaitMs = 5000)
    {
        using var probe = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var deadline = DateTime.UtcNow.AddMilliseconds(maxWaitMs);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await probe.GetAsync("/");
                return;
            }
            catch
            {
                await Task.Delay(50);
            }
        }

        throw new TimeoutException($"Server auf {baseUrl} nicht erreichbar nach {maxWaitMs} ms.");
    }
}
