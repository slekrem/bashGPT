using System.Diagnostics;
using System.Net;
using bashGPT.Core.Configuration;
using bashGPT.Core.Storage;
using bashGPT.Agents;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

public class ServerHost
{
    private readonly SettingsApiHandler _settingsHandler;
    private readonly ChatApiHandler _chatHandler;
    private readonly StreamingChatApiHandler _streamingChatHandler;
    private readonly ChatCancelApiHandler _chatCancelHandler;
    private readonly SessionApiHandler _sessionHandler;
    private readonly AgentApiHandler _agentHandler;
    private readonly ToolApiHandler _toolHandler;
    private readonly LegacyHistoryApiHandler _legacyHistoryHandler;
    private readonly RunningChatRegistry _runningChats;
    private readonly ServerToolSelectionPolicy _toolSelectionPolicy;

    public ServerHost(
        IChatHandler handler,
        ConfigurationService? configService = null,
        SessionStore? sessionStore = null,
        SessionRequestStore? sessionRequestStore = null,
        AgentRegistry? agentRegistry = null,
        ToolRegistry? toolRegistry = null,
        ServerToolSelectionPolicy? toolSelectionPolicy = null)
    {
        _toolSelectionPolicy = toolSelectionPolicy ?? ServerToolSelectionPolicy.FromEnvironment();
        _runningChats = new RunningChatRegistry();
        _settingsHandler = new SettingsApiHandler(configService);
        _chatHandler = new ChatApiHandler(handler, _toolSelectionPolicy, sessionStore, sessionRequestStore, toolRegistry, agentRegistry);
        _streamingChatHandler = new StreamingChatApiHandler(handler, _runningChats, _toolSelectionPolicy, sessionStore, sessionRequestStore, toolRegistry, agentRegistry);
        _chatCancelHandler = new ChatCancelApiHandler(_runningChats);
        _sessionHandler = new SessionApiHandler(sessionStore);
        _agentHandler = new AgentApiHandler(agentRegistry, configService);
        _toolHandler = new ToolApiHandler(toolRegistry);
        _legacyHistoryHandler = new LegacyHistoryApiHandler(sessionStore);
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
            Console.Error.WriteLine($"Failed to start server: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"bashGPT Server running on {prefix}");
        Console.WriteLine("Press Ctrl+C to stop.");

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
                await _legacyHistoryHandler.HandleHistoryAsync(ctx.Response, ct);
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/reset")
            {
                await _legacyHistoryHandler.HandleResetAsync(ctx.Response, ct);
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

            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Not found." }, statusCode: 404);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[server] Unhandled request error: {ex}");
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = ApiErrors.GenericServerError }, statusCode: 500);
        }
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
