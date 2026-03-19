using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BashGPT.Agents;
using BashGPT.Agents.Dev;
using BashGPT.Agents.Shell;
using BashGPT.Server;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für GET /api/agents und GET /api/agents/:id/info-panel.
/// </summary>
public sealed class AgentApiTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private readonly HttpClient _client = new();
    private readonly AgentRegistry _registry = new([new DevAgent(), new ShellAgent()]);
    private ServerHost _server = null!;
    private CancellationTokenSource _cts = null!;
    private Task _serverTask = null!;
    private string _baseUrl = string.Empty;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";
        _client.BaseAddress = new Uri(_baseUrl);

        var options = new ServerOptions(Port: port, NoBrowser: true, Model: null, Verbose: false);
        _server = new ServerHost(_handler, agentRegistry: _registry);
        _cts = new CancellationTokenSource();
        _serverTask = _server.RunAsync(options, _cts.Token);

        await WaitForServerAsync(_baseUrl);
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        try { using var probe = new HttpClient(); await probe.GetAsync($"{_baseUrl}/"); } catch { }
        try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        _client.Dispose();
        _cts.Dispose();
    }

    // ── GET /api/agents ─────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Agents_Returns200WithAgentList()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/agents");

        var agents = json.GetProperty("agents");
        Assert.Equal(JsonValueKind.Array, agents.ValueKind);
        Assert.Equal(2, agents.GetArrayLength());
    }

    [Fact]
    public async Task Get_Agents_ContainsDevAndShellAgent()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/agents");
        var agents = json.GetProperty("agents");

        var ids = Enumerable.Range(0, agents.GetArrayLength())
            .Select(i => agents[i].GetProperty("id").GetString())
            .ToHashSet();

        Assert.Contains("dev", ids);
        Assert.Contains("shell", ids);
    }

    [Fact]
    public async Task Get_Agents_ReturnsOnlyIdAndName()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/agents");
        var agent = json.GetProperty("agents")[0];

        Assert.True(agent.TryGetProperty("id", out _));
        Assert.True(agent.TryGetProperty("name", out _));
        Assert.False(agent.TryGetProperty("systemPrompt", out _));
        Assert.False(agent.TryGetProperty("enabledTools", out _));
    }

    // ── GET /api/agents/:id/info-panel ──────────────────────────────────────

    [Fact]
    public async Task Get_AgentInfoPanel_KnownId_ReturnsMarkdown()
    {
        var response = await _client.GetAsync("/api/agents/dev/info-panel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var markdown = json.GetProperty("markdown").GetString();
        Assert.False(string.IsNullOrWhiteSpace(markdown));
        Assert.Contains("# Dev-Agent", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_AgentInfoPanel_ShellAgent_ReturnsMarkdown()
    {
        var response = await _client.GetAsync("/api/agents/shell/info-panel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var markdown = json.GetProperty("markdown").GetString();
        Assert.Contains("# Shell-Agent", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_AgentInfoPanel_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/agents/unknown-agent/info-panel");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Ohne Registry ────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Agents_WithoutRegistry_Returns503()
    {
        var port = GetFreePort();
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var serverWithoutRegistry = new ServerHost(_handler);
        using var cts = new CancellationTokenSource();
        var task = serverWithoutRegistry.RunAsync(new ServerOptions(port, true, null, false), cts.Token);

        await WaitForServerAsync($"http://127.0.0.1:{port}");

        var response = await client.GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await cts.CancelAsync();
        try { await client.GetAsync("/api/agents"); } catch { }
        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
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
