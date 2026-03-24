using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using bashGPT.Agents;
using bashGPT.Agents.Dev;
using bashGPT.Agents.Shell;
using bashGPT.Server.Services;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für GET /api/agents und GET /api/agents/:id/info-panel.
/// </summary>
public sealed class AgentApiTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private readonly AgentRegistry _registry = new([new DevAgent(), new ShellAgent()]);
    private HttpClient _client = null!;
    private WebApplication _app = null!;

    public async Task InitializeAsync()
    {
        (_app, _client) = await TestServerFactory.CreateAsync(_handler, agentRegistry: _registry);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
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
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.GetAsync("/api/agents");

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
