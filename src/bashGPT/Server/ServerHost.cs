using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Server;

public class ServerHost(PromptHandler handler)
{
    private readonly List<ChatMessage> _history = [];
    private readonly object _historyLock = new();

    public async Task<int> RunAsync(ServerOptions options, CancellationToken ct = default)
    {
        var prefix = $"http://127.0.0.1:{options.Port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.Error.WriteLine($"Server konnte nicht gestartet werden: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"bashGPT Server läuft auf {prefix}");
        Console.WriteLine("Beenden mit Ctrl+C");

        if (!options.NoBrowser)
            TryOpenBrowser(prefix);

        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync();
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(ctx, options, ct), ct);
        }

        listener.Stop();
        return 0;
    }

    private async Task HandleRequestAsync(
        HttpListenerContext ctx,
        ServerOptions options,
        CancellationToken ct)
    {
        try
        {
            var req = ctx.Request;
            var path = req.Url?.AbsolutePath ?? "/";

            if (req.HttpMethod == "GET" && path == "/")
            {
                await WriteResourceAsync(ctx.Response, "bashGPT.Web.index.html", "text/html; charset=utf-8");
                return;
            }

            if (req.HttpMethod == "GET" && path == "/bundle.js")
            {
                await WriteResourceAsync(ctx.Response, "bashGPT.Web.bundle.js", "application/javascript; charset=utf-8");
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/history")
            {
                List<HistoryItem> items;
                lock (_historyLock)
                    items = _history.Select(ToHistoryItem).ToList();
                await WriteJsonAsync(ctx.Response, new { history = items });
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/reset")
            {
                lock (_historyLock)
                    _history.Clear();
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/chat")
            {
                var body = await JsonSerializer.DeserializeAsync<ChatRequest>(
                    req.InputStream,
                    JsonDefaults.Options,
                    ct);

                if (body is null || string.IsNullOrWhiteSpace(body.Prompt))
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Prompt fehlt." }, statusCode: 400);
                    return;
                }

                var historySnapshot = GetHistorySnapshot();
                var requestedMode = ParseExecMode(body.ExecMode) ?? options.ExecMode;
                var chatOpts = new ServerChatOptions(
                    Prompt: body.Prompt.Trim(),
                    History: historySnapshot,
                    Provider: options.Provider,
                    Model: options.Model,
                    NoContext: options.NoContext,
                    IncludeDir: options.IncludeDir,
                    ExecMode: requestedMode,
                    Verbose: options.Verbose || body.Verbose == true,
                    ForceTools: options.ForceTools);

                var result = await handler.RunServerChatAsync(chatOpts, ct);

                AppendToHistory(new ChatMessage(ChatRole.User, body.Prompt.Trim()));
                AppendToHistory(new ChatMessage(ChatRole.Assistant, result.Response));

                await WriteJsonAsync(ctx.Response, new
                {
                    response = result.Response,
                    usedToolCalls = result.UsedToolCalls,
                    logs = result.Logs,
                    commands = result.Commands.Select(c => new
                    {
                        command = c.Command,
                        exitCode = c.ExitCode,
                        output = c.Output,
                        wasExecuted = c.WasExecuted
                    })
                });
                return;
            }

            await WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(ctx.Response, new { error = ex.Message }, statusCode: 500);
        }
    }

    private IReadOnlyList<ChatMessage> GetHistorySnapshot()
    {
        lock (_historyLock)
            return _history.ToList();
    }

    private void AppendToHistory(ChatMessage message)
    {
        lock (_historyLock)
        {
            _history.Add(message);
            if (_history.Count > 40)
                _history.RemoveRange(0, _history.Count - 40);
        }
    }

    private static ExecutionMode? ParseExecMode(string? mode) =>
        mode?.ToLowerInvariant() switch
        {
            "ask" => ExecutionMode.Ask,
            "dry-run" => ExecutionMode.DryRun,
            "auto-exec" => ExecutionMode.AutoExec,
            "no-exec" => ExecutionMode.NoExec,
            _ => null
        };

    private static HistoryItem ToHistoryItem(ChatMessage message) =>
        new(message.RoleString, message.Content);

    private static async Task WriteJsonAsync(HttpListenerResponse response, object payload, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonDefaults.Options);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static async Task WriteResourceAsync(HttpListenerResponse response, string resourceName, string contentType)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }
        response.StatusCode = 200;
        response.ContentType = contentType;
        response.ContentLength64 = stream.Length;
        await stream.CopyToAsync(response.OutputStream);
        response.Close();
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
                return;
            }
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                return;
            }
            if (OperatingSystem.IsLinux())
                Process.Start("xdg-open", url);
        }
        catch
        {
            // Kein Hard-Fail, UI bleibt über URL erreichbar.
        }
    }

    private sealed record ChatRequest(string Prompt, string? ExecMode, bool? Verbose);
    private sealed record HistoryItem(string Role, string Content);
}
