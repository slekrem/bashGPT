using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Server;
using BashGPT.Tools.Execution;
using BashGPT.Tools.Fetch;
using BashGPT.Tools.Filesystem;
using BashGPT.Tools.Shell;

namespace BashGPT.Server.Tests;

public sealed class ServerToolSelectionTests
{
    [Fact]
    public async Task Get_Tools_OnlyReturnsDefaultSelectableTools()
    {
        var handler = new FakePromptHandler();
        var registry = new ToolRegistry([
            new ShellExecTool(),
            new FilesystemReadTool(),
            new FetchTool(),
        ]);

        await using var fixture = await ServerFixture.StartAsync(handler, registry);

        var response = await fixture.Client.GetAsync("/api/tools");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var toolNames = payload.GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .OfType<string>()
            .ToList();

        Assert.Contains("filesystem_read", toolNames);
        Assert.Contains("fetch", toolNames);
        Assert.DoesNotContain("shell_exec", toolNames);
    }

    [Fact]
    public async Task Post_Chat_FiltersDangerousRequestTools_ByDefault()
    {
        var handler = new FakePromptHandler();
        var registry = new ToolRegistry([
            new ShellExecTool(),
            new FilesystemReadTool(),
        ]);

        await using var fixture = await ServerFixture.StartAsync(handler, registry);

        var body = JsonSerializer.Serialize(new
        {
            prompt = "lies die datei",
            enabledTools = new[] { "shell_exec", "filesystem_read" }
        });

        var response = await fixture.Client.PostAsync("/api/chat",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(handler.LastOptions);
        Assert.NotNull(handler.LastOptions!.Tools);

        var toolNames = handler.LastOptions.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("filesystem_read", toolNames);
        Assert.DoesNotContain("shell_exec", toolNames);
    }

    [Fact]
    public async Task Get_Tools_IncludesExplicitlyAllowedDangerousTools()
    {
        var handler = new FakePromptHandler();
        var registry = new ToolRegistry([
            new ShellExecTool(),
            new FilesystemReadTool(),
        ]);
        var policy = new ServerToolSelectionPolicy(["shell_exec"]);

        await using var fixture = await ServerFixture.StartAsync(handler, registry, policy);

        var response = await fixture.Client.GetAsync("/api/tools");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var toolNames = payload.GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .OfType<string>()
            .ToList();

        Assert.Contains("filesystem_read", toolNames);
        Assert.Contains("shell_exec", toolNames);
    }

    private sealed class ServerFixture : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Task _serverTask;
        private readonly string _baseUrl;

        private ServerFixture(HttpClient client, CancellationTokenSource cts, Task serverTask, string baseUrl)
        {
            Client = client;
            _cts = cts;
            _serverTask = serverTask;
            _baseUrl = baseUrl;
        }

        public HttpClient Client { get; }

        public static async Task<ServerFixture> StartAsync(
            FakePromptHandler handler,
            ToolRegistry registry,
            ServerToolSelectionPolicy? policy = null)
        {
            var port = GetFreePort();
            var baseUrl = $"http://127.0.0.1:{port}";
            var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var server = new ServerHost(
                handler,
                toolRegistry: registry,
                toolSelectionPolicy: policy);
            var cts = new CancellationTokenSource();
            var options = new ServerOptions(
                Port: port,
                NoBrowser: true,
                Provider: null,
                Model: null,
                Verbose: false);

            var serverTask = server.RunAsync(options, cts.Token);
            await WaitForServerAsync(baseUrl);

            return new ServerFixture(client, cts, serverTask, baseUrl);
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            try
            {
                using var probe = new HttpClient();
                await probe.GetAsync($"{_baseUrl}/").ConfigureAwait(false);
            }
            catch { }

            try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch { }

            Client.Dispose();
            _cts.Dispose();
        }

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
}
