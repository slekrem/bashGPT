using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BashGPT.Configuration;
using BashGPT.Server;
using BashGPT.Shell;

namespace BashGPT.Server.Tests;

/// <summary>
/// Integration-Tests für ServerHost: starten den echten HTTP-Listener auf einem
/// zufälligen Port und prüfen alle API-Endpunkte ohne echte LLM-Verbindung.
/// </summary>
public sealed class ServerHostTests : IAsyncLifetime
{
    private readonly FakePromptHandler _handler = new();
    private readonly HttpClient _client = new();
    private ServerHost _server = null!;
    private CancellationTokenSource _cts = null!;
    private Task _serverTask = null!;
    private string _baseUrl = string.Empty;

    // ── Setup / Teardown ────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
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
            ExecMode: ExecutionMode.NoExec,
            Verbose: false,
            ForceTools: null);

        _server = new ServerHost(_handler);
        _cts = new CancellationTokenSource();
        _serverTask = _server.RunAsync(options, _cts.Token);

        // Kurz warten, bis der Listener bereit ist
        await WaitForServerAsync(_baseUrl);
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        // GetContextAsync() ignoriert CancellationToken – wir senden eine letzte
        // Probe-Anfrage, damit der Listener-Loop die CT-Prüfung erreicht.
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
        _handler.NextResult = new("Das ist eine Test-Antwort.", [], [], false);

        var response = await PostChatAsync("Was ist Zeit?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Das ist eine Test-Antwort.", json.GetProperty("response").GetString());
        Assert.False(json.GetProperty("usedToolCalls").GetBoolean());
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
        var body = JsonSerializer.Serialize(new { execMode = "no-exec" });
        var response = await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Chat_PassesExecModeToHandler()
    {
        await SendChatRaw("Test", execMode: "auto-exec");

        Assert.Equal(ExecutionMode.AutoExec, _handler.LastOptions?.ExecMode);
    }

    [Fact]
    public async Task Post_Chat_UnknownExecMode_FallsBackToServerDefault()
    {
        await SendChatRaw("Test", execMode: "unknown-mode");

        // ServerOptions.ExecMode = NoExec (gesetzt in InitializeAsync)
        Assert.Equal(ExecutionMode.NoExec, _handler.LastOptions?.ExecMode);
    }

    [Fact]
    public async Task Post_Chat_WithCommands_ReturnsCommandsInResponse()
    {
        _handler.NextResult = new(
            Response: "Habe ls ausgeführt.",
            Commands: [new CommandResult("ls -la", 0, "total 0\ndrwxr-xr-x  2 user user   40 Feb 28 12:00 .", true)],
            Logs: [],
            UsedToolCalls: true);

        var response = await PostChatAsync("Liste Dateien auf");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("usedToolCalls").GetBoolean());

        var commands = json.GetProperty("commands");
        Assert.Equal(1, commands.GetArrayLength());
        Assert.Equal("ls -la", commands[0].GetProperty("command").GetString());
        Assert.Equal(0, commands[0].GetProperty("exitCode").GetInt32());
        Assert.True(commands[0].GetProperty("wasExecuted").GetBoolean());
    }

    [Fact]
    public async Task Post_Chat_HandlerThrows_Returns500()
    {
        _handler.NextException = new InvalidOperationException("Simulierter Fehler");

        var response = await PostChatAsync("Irgendwas");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out _));
    }

    // ── Exec-Mode-Wechsel ────────────────────────────────────────────────────

    [Theory]
    [InlineData("ask", ExecutionMode.Ask)]
    [InlineData("dry-run", ExecutionMode.DryRun)]
    [InlineData("auto-exec", ExecutionMode.AutoExec)]
    [InlineData("no-exec", ExecutionMode.NoExec)]
    public async Task Post_Chat_AllExecModes_AreParsedCorrectly(string modeString, ExecutionMode expected)
    {
        await SendChatRaw("Test", execMode: modeString);

        Assert.Equal(expected, _handler.LastOptions?.ExecMode);
    }

    // ── 404 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/not-existing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> PostChatAsync(string prompt, string execMode = "no-exec")
    {
        return await SendChatRaw(prompt, execMode);
    }

    private async Task<HttpResponseMessage> SendChatRaw(string? prompt = null, string? execMode = null)
    {
        var payload = new Dictionary<string, object?>();
        if (prompt is not null) payload["prompt"] = prompt;
        if (execMode is not null) payload["execMode"] = execMode;

        var body = JsonSerializer.Serialize(payload);
        return await _client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
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
