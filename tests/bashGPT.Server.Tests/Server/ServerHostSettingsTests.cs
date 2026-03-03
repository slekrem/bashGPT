using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Shell;

namespace BashGPT.Tests.Server;

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
            Provider: null,
            Model: null,
            NoContext: true,
            IncludeDir: false,
            ExecMode: ExecutionMode.Ask,
            Verbose: false,
            ForceTools: false);

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
        var options = new ServerOptions(port, true, null, null, true, false, ExecutionMode.Ask, false, false);
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
        Assert.True(json.TryGetProperty("hasApiKey", out _));
        Assert.True(json.TryGetProperty("ollamaHost", out _));
        Assert.True(json.TryGetProperty("execMode", out _));
        Assert.True(json.TryGetProperty("forceTools", out _));

        // Standardprovider ist Ollama
        Assert.Equal("ollama", provider.GetString());
    }

    // ── PUT /api/settings ───────────────────────────────────────────────────

    [Fact]
    public async Task Put_Settings_UpdatesProvider_And_Persists()
    {
        var body = JsonSerializer.Serialize(new { provider = "cerebras", model = "test-model" });
        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Config-Datei prüfen
        var config = await _configService.LoadAsync();
        Assert.Equal(ProviderType.Cerebras, config.DefaultProvider);
        Assert.Equal("test-model", config.Cerebras.Model);
    }

    [Fact]
    public async Task Put_Settings_NestedProviderPayload_UpdatesBothWithoutOverwrite()
    {
        var body = JsonSerializer.Serialize(new
        {
            provider = "ollama",
            cerebras = new { model = "cerebras-model-x" },
            ollama = new { model = "ollama-model-y", host = "http://ollama.local:11434" },
        });

        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal(ProviderType.Ollama, config.DefaultProvider);
        Assert.Equal("cerebras-model-x", config.Cerebras.Model);
        Assert.Equal("ollama-model-y", config.Ollama.Model);
        Assert.Equal("http://ollama.local:11434", config.Ollama.BaseUrl);
    }

    [Fact]
    public async Task Put_Settings_CerebrasAdvancedOptions_ArePersisted()
    {
        var body = JsonSerializer.Serialize(new
        {
            provider = "cerebras",
            cerebras = new
            {
                model = "cerebras-model-x",
                temperature = 0.2,
                topP = 0.95,
                maxCompletionTokens = 8192,
                seed = 1234,
                reasoningEffort = "medium",
            },
        });

        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal(ProviderType.Cerebras, config.DefaultProvider);
        Assert.Equal("cerebras-model-x", config.Cerebras.Model);
        Assert.Equal(0.2, config.Cerebras.Temperature);
        Assert.Equal(0.95, config.Cerebras.TopP);
        Assert.Equal(8192, config.Cerebras.MaxCompletionTokens);
        Assert.Equal(1234, config.Cerebras.Seed);
        Assert.Equal("medium", config.Cerebras.ReasoningEffort);
    }

    [Fact]
    public async Task Put_Settings_OllamaAdvancedOptions_ArePersisted()
    {
        var body = JsonSerializer.Serialize(new
        {
            provider = "ollama",
            ollama = new
            {
                model = "ollama-model-y",
                host = "http://ollama.local:11434",
                temperature = 0.3,
                topP = 0.9,
                seed = 99,
            },
        });

        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var config = await _configService.LoadAsync();
        Assert.Equal(ProviderType.Ollama, config.DefaultProvider);
        Assert.Equal("ollama-model-y", config.Ollama.Model);
        Assert.Equal("http://ollama.local:11434", config.Ollama.BaseUrl);
        Assert.Equal(0.3, config.Ollama.Temperature);
        Assert.Equal(0.9, config.Ollama.TopP);
        Assert.Equal(99, config.Ollama.Seed);
    }

    [Fact]
    public async Task Put_Settings_EmptyApiKey_DoesNotOverwriteExisting()
    {
        // Zuerst einen API-Key setzen
        await _configService.SetAsync("cerebras.apiKey", "geheimer-key");

        // PUT mit leerem apiKey senden
        var body = JsonSerializer.Serialize(new { apiKey = "" });
        await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        // Key darf nicht überschrieben worden sein
        var config = await _configService.LoadAsync();
        Assert.Equal("geheimer-key", config.Cerebras.ApiKey);
    }

    [Fact]
    public async Task Put_Settings_UpdatesExecMode_InMemory()
    {
        // ExecMode auf auto-exec setzen
        var body = JsonSerializer.Serialize(new { execMode = "auto-exec" });
        var putResponse = await _client.PutAsync("/api/settings",
            new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // GET prüft, ob der In-Memory-Wert aktualisiert wurde
        var getResponse = await _client.GetFromJsonAsync<JsonElement>("/api/settings");
        Assert.Equal("auto-exec", getResponse.GetProperty("execMode").GetString());
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
