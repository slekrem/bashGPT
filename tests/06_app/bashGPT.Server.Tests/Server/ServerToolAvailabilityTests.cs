using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using bashGPT.Tools.Registration;
using bashGPT.Tools.Fetch;
using bashGPT.Tools.Filesystem;
using bashGPT.Tools.Shell.Shells;
using bashGPT.Server;

namespace bashGPT.Server.Tests;

/// <summary>
/// Integration tests for the new server tool availability behavior:
/// all registered tools are exposed without a server-side allowlist,
/// and enabledTools from requests pass through unfiltered.
/// </summary>
public sealed class ServerToolAvailabilityTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private HttpClient _client = null!;
    private WebApplication _app = null!;

    public async Task InitializeAsync()
    {
        var registry = new ToolRegistry([
            new ShellExecTool(),
            new FilesystemReadTool(),
            new FetchTool(),
        ]);

        (_app, _client) = await TestServerFactory.CreateAsync(_handler, toolRegistry: registry);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
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
        var (app, client) = await TestServerFactory.CreateAsync(_handler);

        var response = await client.GetAsync("/api/tools");

        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

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
}
