using System.Diagnostics;
using System.Net;
using BashGPT.Agents;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Storage;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

public class ServerHost
{
    private readonly ContextApiHandler _contextHandler;
    private readonly SettingsApiHandler _settingsHandler;
    private readonly ChatApiHandler _chatHandler;
    private readonly StreamingChatApiHandler _streamingChatHandler;
    private readonly ChatCancelApiHandler _chatCancelHandler;
    private readonly SessionApiHandler _sessionHandler;
    private readonly AgentApiHandler _agentHandler;
    private readonly ToolApiHandler _toolHandler;
    private readonly ConfigurationService? _configService;
    private readonly AgentRegistry? _agentRegistry;
    private readonly SessionStore? _sessionStore;
    private readonly ToolRegistry? _toolRegistry;
    private readonly RunningChatRegistry _runningChats;
    private readonly ServerToolSelectionPolicy _toolSelectionPolicy;

    public ServerHost(
        IPromptHandler handler,
        ConfigurationService? configService = null,
        SessionStore? sessionStore = null,
        AgentRegistry? agentRegistry = null,
        ToolRegistry? toolRegistry = null,
        ServerToolSelectionPolicy? toolSelectionPolicy = null)
    {
        _configService = configService;
        _agentRegistry = agentRegistry;
        _sessionStore = sessionStore;
        _toolRegistry = toolRegistry;
        _toolSelectionPolicy = toolSelectionPolicy ?? ServerToolSelectionPolicy.FromEnvironment();
        _runningChats = new RunningChatRegistry();
        _contextHandler = new ContextApiHandler();
        _settingsHandler = new SettingsApiHandler(configService);
        _chatHandler = new ChatApiHandler(handler, _toolSelectionPolicy, sessionStore, toolRegistry, agentRegistry);
        _streamingChatHandler = new StreamingChatApiHandler(handler, _runningChats, _toolSelectionPolicy, sessionStore, toolRegistry, agentRegistry);
        _chatCancelHandler = new ChatCancelApiHandler(_runningChats);
        _sessionHandler = new SessionApiHandler(sessionStore);
        _agentHandler = new AgentApiHandler(agentRegistry, configService);
        _toolHandler = new ToolApiHandler(toolRegistry, _toolSelectionPolicy);
    }

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

        Console.WriteLine($"bashGPT Server laeuft auf {prefix}");
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

    private async Task HandleRequestAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        try
        {
            var req = ctx.Request;
            var path = req.Url?.AbsolutePath ?? "/";

            if (req.HttpMethod == "GET" && path == "/")
            {
                var found = await ApiResponse.TryWriteResourceAsync(ctx.Response, "bashGPT.Web.index.html", "text/html; charset=utf-8");
                if (!found)
                {
                    await ApiResponse.WriteTextAsync(
                        ctx.Response,
                        "<!doctype html><html><head><meta charset=\"utf-8\"><title>bashGPT</title></head><body><div id=\"app\"></div><script src=\"/bundle.js\"></script></body></html>",
                        "text/html; charset=utf-8");
                }
                return;
            }

            if (req.HttpMethod == "GET" && path == "/bundle.js")
            {
                var found = await ApiResponse.TryWriteResourceAsync(ctx.Response, "bashGPT.Web.bundle.js", "application/javascript; charset=utf-8");
                if (!found)
                {
                    await ApiResponse.WriteTextAsync(
                        ctx.Response,
                        "console.warn('bashGPT frontend bundle not embedded.');",
                        "application/javascript; charset=utf-8");
                }
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/history")
            {
                await WriteLegacyHistoryAsync(ctx.Response, ct);
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/reset")
            {
                await ResetLegacyHistoryAsync(ctx.Response, ct);
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/context")
            {
                await _contextHandler.HandleAsync(ctx.Response, ct);
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/version")
            {
                await new VersionApiHandler().HandleAsync(ctx.Response, ct);
                return;
            }

            if (path.StartsWith("/api/settings", StringComparison.Ordinal))
            {
                await _settingsHandler.HandleAsync(ctx, ct);
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/chat/stream")
            {
                await _streamingChatHandler.HandleAsync(ctx, options, ct);
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/chat/cancel")
            {
                await _chatCancelHandler.HandleAsync(ctx, ct);
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/chat")
            {
                await _chatHandler.HandleAsync(ctx, options, ct);
                return;
            }

            if (path.StartsWith("/api/sessions", StringComparison.Ordinal))
            {
                await _sessionHandler.HandleAsync(ctx, ct);
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/tools")
            {
                await _toolHandler.HandleAsync(ctx.Response, ct);
                return;
            }

            if (path.StartsWith("/api/agents", StringComparison.Ordinal))
            {
                await _agentHandler.HandleAsync(ctx, ct);
                return;
            }

            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[server] Unbehandelter Request-Fehler: {ex}");
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = ApiErrors.GenericServerError }, statusCode: 500);
        }
    }

    private async Task WriteLegacyHistoryAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (_sessionStore is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { history = Array.Empty<object>() });
            return;
        }

        var sessions = await _sessionStore.LoadAllAsync();
        var latest = sessions
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { history = Array.Empty<object>() });
            return;
        }

        var session = await _sessionStore.LoadAsync(latest.Id);
        var history = session?.Messages
            .Where(m => (m.Role == "user" || m.Role == "assistant" || m.Role == "tool") && !string.IsNullOrEmpty(m.Content))
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList()
            ?? [];

        await ApiResponse.WriteJsonAsync(response, new { history });
    }

    private async Task ResetLegacyHistoryAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (_sessionStore is not null)
            await _sessionStore.ClearAsync();

        await ApiResponse.WriteJsonAsync(response, new { ok = true });
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS()) { Process.Start("open", url); return; }
            if (OperatingSystem.IsWindows()) { Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true }); return; }
            if (OperatingSystem.IsLinux()) Process.Start("xdg-open", url);
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                      or System.ComponentModel.Win32Exception
                                      or IOException)
        {
            _ = ex;
        }
    }
}
