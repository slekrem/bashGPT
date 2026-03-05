using System.Diagnostics;
using System.Net;
using BashGPT.Agents;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;

namespace BashGPT.Server;

public class ServerHost
{
    private readonly ServerState               _state;
    private readonly LegacyHistory             _legacyHistory;
    private readonly ContextApiHandler         _contextHandler;
    private readonly SettingsApiHandler        _settingsHandler;
    private readonly ChatApiHandler            _chatHandler;
    private readonly StreamingChatApiHandler   _streamingChatHandler;
    private readonly SessionApiHandler         _sessionHandler;
    private readonly AgentApiHandler           _agentHandler;
    private readonly ConfigurationService?     _configService;
    private readonly AgentStore?               _agentStore;
    private readonly SessionStore?             _sessionStore;

    public ServerHost(
        IPromptHandler handler,
        ConfigurationService? configService = null,
        SessionStore? sessionStore = null,
        AgentStore? agentStore = null)
    {
        _configService        = configService;
        _agentStore           = agentStore;
        _sessionStore         = sessionStore;
        _state                = new ServerState();
        _legacyHistory        = new LegacyHistory();
        _contextHandler       = new ContextApiHandler();
        _settingsHandler      = new SettingsApiHandler(configService, _state);
        _chatHandler          = new ChatApiHandler(handler, _state, _legacyHistory, sessionStore);
        _streamingChatHandler = new StreamingChatApiHandler(handler, _state, _legacyHistory, sessionStore);
        _sessionHandler       = new SessionApiHandler(sessionStore, _legacyHistory);
        _agentHandler         = new AgentApiHandler(agentStore);
    }

    public async Task<int> RunAsync(ServerOptions options, CancellationToken ct = default)
    {
        AppConfig? appConfig = null;
        if (_configService is not null)
            appConfig = await _configService.LoadAsync();

        _state.ExecMode   = options.ExecMode   ?? appConfig?.DefaultExecMode   ?? ExecutionMode.Ask;
        _state.ForceTools = options.ForceTools ?? appConfig?.DefaultForceTools ?? false;

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

        if (_agentStore is not null)
        {
            ILlmProvider? agentProvider = null;
            if (_configService is not null)
                try { agentProvider = ProviderFactory.Create(appConfig ?? await _configService.LoadAsync()); }
                catch (Exception ex) { Console.Error.WriteLine($"[WARN] LLM für Agenten nicht verfügbar: {ex.Message}"); }

            var runner = new AgentRunner(_agentStore,
                [new GitStatusCheck(), new HttpStatusCheck(), new BitcoinPriceCheck(), new LlmAgentCheck(agentProvider, _sessionStore)],
                agentProvider, _sessionStore);
            _ = Task.Run(() => runner.RunAsync(ct), ct);
            Console.WriteLine($"Agent-Runner gestartet{(agentProvider is not null ? $" | LLM: {agentProvider.Name} ({agentProvider.Model})" : " | LLM: nicht konfiguriert")}.");
        }

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

    // ── Routing ─────────────────────────────────────────────────────────────

    private async Task HandleRequestAsync(HttpListenerContext ctx, ServerOptions options, CancellationToken ct)
    {
        try
        {
            var req  = ctx.Request;
            var path = req.Url?.AbsolutePath ?? "/";

            if (req.HttpMethod == "GET"  && path == "/")
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

            if (req.HttpMethod == "GET"  && path == "/bundle.js")
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

            // @deprecated – Nur noch für Frontend-Kompatibilität; neue Clients nutzen /api/sessions
            if (req.HttpMethod == "GET"  && path == "/api/history")
            { await ApiResponse.WriteJsonAsync(ctx.Response, new { history = _legacyHistory.GetItems() }); return; }

            // @deprecated – Nur noch für Frontend-Kompatibilität; neue Clients nutzen /api/sessions/clear
            if (req.HttpMethod == "POST" && path == "/api/reset")
            {
                _legacyHistory.Clear();
                await ApiResponse.WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (req.HttpMethod == "GET"  && path == "/api/context")
            { await _contextHandler.HandleAsync(ctx.Response, ct); return; }

            if (path.StartsWith("/api/settings", StringComparison.Ordinal))
            { await _settingsHandler.HandleAsync(ctx, ct); return; }

            if (req.HttpMethod == "POST" && path == "/api/chat/stream")
            { await _streamingChatHandler.HandleAsync(ctx, options, ct); return; }

            if (req.HttpMethod == "POST" && path == "/api/chat")
            { await _chatHandler.HandleAsync(ctx, options, ct); return; }

            if (path.StartsWith("/api/sessions", StringComparison.Ordinal))
            { await _sessionHandler.HandleAsync(ctx, ct); return; }

            if (path.StartsWith("/api/agents", StringComparison.Ordinal))
            { await _agentHandler.HandleAsync(ctx, ct); return; }

            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
        }
        catch (Exception ex)
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = ex.Message }, statusCode: 500);
        }
    }

    // ── Browser öffnen ───────────────────────────────────────────────────────

    private static void TryOpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS())   { Process.Start("open", url); return; }
            if (OperatingSystem.IsWindows()) { Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true }); return; }
            if (OperatingSystem.IsLinux())     Process.Start("xdg-open", url);
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                      or System.ComponentModel.Win32Exception
                                      or IOException)
        {
            // Kein Hard-Fail, UI bleibt über URL erreichbar.
            _ = ex;
        }
    }
}
