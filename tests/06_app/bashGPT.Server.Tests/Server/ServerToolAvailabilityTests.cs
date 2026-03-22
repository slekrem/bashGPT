using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Registration;
using bashGPT.Tools.Fetch;
using bashGPT.Tools.Filesystem;
using bashGPT.Tools.Shell.Shells;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration tests for the new server tool availability behavior:
/// all registered tools are exposed without a server-side allowlist,
/// and enabledTools from requests pass through unfiltered.
/// </summary>
public sealed class ServerToolAvailabilityTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private readonly HttpClient _client = new();
    private ServerHost _server = null!;
    private CancellationTokenSource _cts = null!;
    private Task _serverTask = null!;
    private string _baseUrl = string.Empty;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";
        _client.BaseAddress = new Uri(_baseUrl);

        var registry = new ToolRegistry([
            new ShellExecTool(),
            new FilesystemReadTool(),
            new FetchTool(),
        ]);

        _server = new ServerHost(_handler, toolRegistry: registry);
        _cts = new CancellationTokenSource();
        _serverTask = _server.RunAsync(new ServerOptions(Port: port, NoBrowser: true, Model: null, Verbose: false), _cts.Token);

        await WaitForServerAsync(_baseUrl);
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        try { using var probe = new HttpClient(); await probe.GetAsync($"{_baseUrl}/").ConfigureAwait(false); } catch { }
        try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        _client.Dispose();
        _cts.Dispose();
    }

    // ── GET /api/tools ───────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Tools_ReturnsAllRegisteredTools()
    {
        var response = await _client.GetAsync("/api/tools");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var toolNames = payload.GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .OfType<string>()
            .ToList();

        Assert.Contains("shell_exec", toolNames);
        Assert.Contains("filesystem_read", toolNames);
        Assert.Contains("fetch", toolNames);
    }

    [Fact]
    public async Task Get_Tools_NoRegistry_ReturnsEmptyList()
    {
        var port = GetFreePort();
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var server = new ServerHost(_handler);
        using var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(new ServerOptions(Port: port, NoBrowser: true, Model: null, Verbose: false), cts.Token);
        await WaitForServerAsync($"http://127.0.0.1:{port}");

        var response = await client.GetAsync("/api/tools");

        await cts.CancelAsync();
        try { using var probe = new HttpClient(); await probe.GetAsync($"http://127.0.0.1:{port}/"); } catch { }
        try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, payload.GetProperty("tools").GetArrayLength());
    }

    // ── POST /api/chat ───────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Chat_EnabledTools_PassThroughUnfiltered()
    {
        var body = JsonSerializer.Serialize(new
        {
            prompt = "führe befehl aus",
            enabledTools = new[] { "shell_exec", "filesystem_read" }
        });

        var response = await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_handler.LastOptions?.Tools);

        var toolNames = _handler.LastOptions!.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("shell_exec", toolNames);
        Assert.Contains("filesystem_read", toolNames);
    }

    [Fact]
    public async Task Post_Chat_AllToolsPassThrough_WhenAllRequested()
    {
        var body = JsonSerializer.Serialize(new
        {
            prompt = "tue etwas",
            enabledTools = new[] { "shell_exec", "filesystem_read", "fetch" }
        });

        var response = await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_handler.LastOptions?.Tools);

        var toolNames = _handler.LastOptions!.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("shell_exec", toolNames);
        Assert.Contains("filesystem_read", toolNames);
        Assert.Contains("fetch", toolNames);
    }

    // ── POST /api/chat/stream ────────────────────────────────────────────────

    [Fact]
    public async Task Post_ChatStream_EnabledTools_PassThroughUnfiltered()
    {
        var body = JsonSerializer.Serialize(new
        {
            prompt = "stream etwas",
            enabledTools = new[] { "shell_exec", "fetch" }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_handler.LastOptions?.Tools);

        var toolNames = _handler.LastOptions!.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("shell_exec", toolNames);
        Assert.Contains("fetch", toolNames);
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
            try { await probe.GetAsync("/"); return; }
            catch { await Task.Delay(50); }
        }

        throw new TimeoutException($"Server auf {baseUrl} nicht erreichbar nach {maxWaitMs} ms.");
    }
}
