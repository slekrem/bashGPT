using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;

namespace BashGPT.Server;

public class ServerHost(
    IPromptHandler handler,
    ConfigurationService? configService = null,
    string? historyFile = null,
    SessionStore? sessionStore = null)
{
    private readonly List<ChatMessage> _history = [];
    private readonly object _historyLock = new();
    private volatile ExecutionMode _execMode = ExecutionMode.Ask;
    private volatile bool _forceTools = false;

    public async Task<int> RunAsync(ServerOptions options, CancellationToken ct = default)
    {
        _execMode = options.ExecMode;
        _forceTools = options.ForceTools;

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

        if (historyFile is not null)
            await LoadHistoryFromFileAsync(historyFile);

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
                await PersistHistoryAsync();
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/settings")
            {
                if (configService is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
                    return;
                }
                var config = await configService.LoadAsync();
                var activeModel = config.DefaultProvider == ProviderType.Cerebras
                    ? config.Cerebras.Model
                    : config.Ollama.Model;
                await WriteJsonAsync(ctx.Response, new
                {
                    provider = config.DefaultProvider.ToString().ToLower(),
                    model = activeModel,
                    hasApiKey = config.Cerebras.ApiKey is not null,
                    ollamaHost = config.Ollama.BaseUrl,
                    execMode = ExecModeToString(_execMode),
                    forceTools = _forceTools
                });
                return;
            }

            if (req.HttpMethod == "PUT" && path == "/api/settings")
            {
                if (configService is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
                    return;
                }
                var body = await JsonSerializer.DeserializeAsync<SettingsRequest>(
                    req.InputStream,
                    JsonDefaults.Options,
                    ct);
                if (body is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Ungültiger Request-Body." }, statusCode: 400);
                    return;
                }
                var config = await configService.LoadAsync();
                var providerType = ParseProviderType(body.Provider);
                if (providerType is not null) config.DefaultProvider = providerType.Value;
                if (body.Model is not null)
                {
                    if (config.DefaultProvider == ProviderType.Cerebras) config.Cerebras.Model = body.Model;
                    else config.Ollama.Model = body.Model;
                }
                if (!string.IsNullOrEmpty(body.ApiKey)) config.Cerebras.ApiKey = body.ApiKey;
                if (body.OllamaHost is not null) config.Ollama.BaseUrl = body.OllamaHost;
                if (body.ExecMode is not null) _execMode = ParseExecMode(body.ExecMode) ?? _execMode;
                if (body.ForceTools is bool ft) _forceTools = ft;
                await configService.SaveAsync(config);
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/settings/test")
            {
                if (configService is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Kein ConfigurationService verfügbar." }, statusCode: 503);
                    return;
                }
                var config = await configService.LoadAsync();
                var provider = ProviderFactory.Create(config);
                var sw = Stopwatch.StartNew();
                try
                {
                    await provider.CompleteAsync([new ChatMessage(ChatRole.User, "Hi")], ct);
                    sw.Stop();
                    await WriteJsonAsync(ctx.Response, new { ok = true, latencyMs = (int)sw.ElapsedMilliseconds });
                }
                catch (LlmProviderException ex)
                {
                    await WriteJsonAsync(ctx.Response, new { ok = false, error = ex.Message });
                }
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

                // Session-basierte History laden, falls sessionId übergeben und SessionStore verfügbar
                IReadOnlyList<ChatMessage> historySnapshot;
                if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
                {
                    var session = await sessionStore.LoadAsync(body.SessionId);
                    historySnapshot = session?.Messages
                        .Where(m => m.Role is "user" or "assistant")
                        .Select(m => new ChatMessage(
                            m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                            m.Content))
                        .ToList() ?? [];
                }
                else
                {
                    historySnapshot = GetHistorySnapshot();
                }

                var requestedMode = ParseExecMode(body.ExecMode) ?? _execMode;
                var chatOpts = new ServerChatOptions(
                    Prompt: body.Prompt.Trim(),
                    History: historySnapshot,
                    Provider: options.Provider,
                    Model: options.Model,
                    NoContext: options.NoContext,
                    IncludeDir: options.IncludeDir,
                    ExecMode: requestedMode,
                    Verbose: options.Verbose || body.Verbose == true,
                    ForceTools: _forceTools);

                var result = await handler.RunServerChatAsync(chatOpts, ct);

                var shellCtx = new SessionShellContext
                {
                    User = Environment.UserName,
                    Host = Environment.MachineName,
                    Cwd  = Environment.CurrentDirectory,
                };

                // Session-basiertes Persistieren
                if (sessionStore is not null && !string.IsNullOrWhiteSpace(body.SessionId))
                {
                    var session = await sessionStore.LoadAsync(body.SessionId);
                    var newMessages = new List<SessionMessage>
                    {
                        new() { Role = "user",      Content = body.Prompt.Trim(), ExecMode = body.ExecMode },
                        new() { Role = "assistant",  Content = result.Response,
                            Commands = result.Commands.Count > 0
                                ? result.Commands.Select(c => new SessionCommand
                                {
                                    Command = c.Command, ExitCode = c.ExitCode,
                                    Output = c.Output, WasExecuted = c.WasExecuted,
                                }).ToList()
                                : null },
                    };

                    var existingMessages = session?.Messages ?? [];
                    var allMessages = existingMessages.Concat(newMessages).ToList();
                    var title = allMessages.FirstOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "Chat";
                    if (title.Length > 40) title = title[..40] + "…";

                    var now = DateTime.UtcNow.ToString("o");
                    await sessionStore.UpsertAsync(new SessionRecord
                    {
                        Id           = body.SessionId,
                        Title        = title,
                        CreatedAt    = session?.CreatedAt ?? now,
                        UpdatedAt    = now,
                        Messages     = allMessages,
                        ShellContext = shellCtx,
                    });
                }
                else
                {
                    // Fallback: globale In-Memory-History (legacy)
                    AppendToHistory(new ChatMessage(ChatRole.User, body.Prompt.Trim()));
                    AppendToHistory(new ChatMessage(ChatRole.Assistant, result.Response));
                    await PersistHistoryAsync();
                }

                await WriteJsonAsync(ctx.Response, new
                {
                    response = result.Response,
                    usedToolCalls = result.UsedToolCalls,
                    logs = result.Logs,
                    shellContext = new { user = shellCtx.User, host = shellCtx.Host, cwd = shellCtx.Cwd },
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

            // ── Session-API ───────────────────────────────────────────────────

            if (sessionStore is not null && req.HttpMethod == "GET" && path == "/api/sessions")
            {
                var sessions = await sessionStore.LoadAllAsync();
                await WriteJsonAsync(ctx.Response, new
                {
                    sessions = sessions.Select(s => new
                    {
                        id = s.Id, title = s.Title,
                        createdAt = s.CreatedAt, updatedAt = s.UpdatedAt,
                    })
                });
                return;
            }

            if (sessionStore is not null && req.HttpMethod == "POST" && path == "/api/sessions")
            {
                var now = DateTime.UtcNow.ToString("o");
                var newSession = new SessionRecord
                {
                    Id = $"s-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", Title = "Neuer Chat",
                    CreatedAt = now, UpdatedAt = now,
                };
                await sessionStore.UpsertAsync(newSession);
                await WriteJsonAsync(ctx.Response, new
                {
                    id = newSession.Id, title = newSession.Title,
                    createdAt = newSession.CreatedAt, updatedAt = newSession.UpdatedAt,
                });
                return;
            }

            if (sessionStore is not null && req.HttpMethod == "POST" && path == "/api/sessions/clear")
            {
                await sessionStore.ClearAsync();
                lock (_historyLock) _history.Clear();
                await PersistHistoryAsync();
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (sessionStore is not null && req.HttpMethod == "GET"
                && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
            {
                var id = path["/api/sessions/".Length..];
                var session = await sessionStore.LoadAsync(id);
                if (session is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Session nicht gefunden." }, statusCode: 404);
                    return;
                }
                await WriteJsonAsync(ctx.Response, session);
                return;
            }

            if (sessionStore is not null && req.HttpMethod == "PUT"
                && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
            {
                var id = path["/api/sessions/".Length..];
                var body = await JsonSerializer.DeserializeAsync<SessionRecord>(
                    req.InputStream, JsonDefaults.Options, ct);
                if (body is null)
                {
                    await WriteJsonAsync(ctx.Response, new { error = "Ungültiger Body." }, statusCode: 400);
                    return;
                }
                body.Id = id;
                body.UpdatedAt = DateTime.UtcNow.ToString("o");
                // Fehlende Felder aus der bestehenden Session übernehmen (verhindert Datenverlust)
                if (string.IsNullOrEmpty(body.CreatedAt) || string.IsNullOrEmpty(body.Title))
                {
                    var existing = await sessionStore.LoadAsync(id);
                    if (string.IsNullOrEmpty(body.CreatedAt))
                        body.CreatedAt = existing?.CreatedAt ?? body.UpdatedAt;
                    if (string.IsNullOrEmpty(body.Title))
                        body.Title = existing?.Title ?? "Chat";
                }
                await sessionStore.UpsertAsync(body);
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            if (sessionStore is not null && req.HttpMethod == "DELETE"
                && path.StartsWith("/api/sessions/", StringComparison.Ordinal))
            {
                var id = path["/api/sessions/".Length..];
                await sessionStore.DeleteAsync(id);
                await WriteJsonAsync(ctx.Response, new { ok = true });
                return;
            }

            await WriteJsonAsync(ctx.Response, new { error = "Nicht gefunden." }, statusCode: 404);
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(ctx.Response, new { error = ex.Message }, statusCode: 500);
        }
    }

    private async Task LoadHistoryFromFileAsync(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var items = JsonSerializer.Deserialize<List<HistoryItem>>(json, JsonDefaults.Options) ?? [];
            var messages = items
                .Select(item => item.Role switch
                {
                    "user"      => new ChatMessage(ChatRole.User,      item.Content),
                    "assistant" => new ChatMessage(ChatRole.Assistant,  item.Content),
                    _           => (ChatMessage?)null
                })
                .Where(m => m is not null)
                .Select(m => m!)
                .ToList();
            lock (_historyLock)
            {
                _history.Clear();
                _history.AddRange(messages);
                if (_history.Count > 40)
                    _history.RemoveRange(0, _history.Count - 40);
            }
        }
        catch { /* beschädigte Datei ignorieren – Neustart mit leerem Verlauf */ }
    }

    private async Task PersistHistoryAsync()
    {
        if (historyFile is null) return;
        try
        {
            List<HistoryItem> items;
            lock (_historyLock)
                items = _history.Select(ToHistoryItem).ToList();
            var dir = Path.GetDirectoryName(historyFile)!;
            Directory.CreateDirectory(dir);
            var serialized = JsonSerializer.Serialize(items, JsonDefaults.Options);
            await File.WriteAllTextAsync(historyFile, serialized);
        }
        catch { /* Schreibfehler ignorieren */ }
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

    private static ProviderType? ParseProviderType(string? provider) =>
        provider?.ToLowerInvariant() switch
        {
            "ollama"   => ProviderType.Ollama,
            "cerebras" => ProviderType.Cerebras,
            _          => null
        };

    private static string ExecModeToString(ExecutionMode mode) =>
        mode switch
        {
            ExecutionMode.Ask      => "ask",
            ExecutionMode.DryRun   => "dry-run",
            ExecutionMode.AutoExec => "auto-exec",
            ExecutionMode.NoExec   => "no-exec",
            _                      => "ask"
        };

    private static ExecutionMode? ParseExecMode(string? mode) =>
        mode?.ToLowerInvariant() switch
        {
            "ask"       => ExecutionMode.Ask,
            "dry-run"   => ExecutionMode.DryRun,
            "auto-exec" => ExecutionMode.AutoExec,
            "no-exec"   => ExecutionMode.NoExec,
            _           => null
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

    private sealed record SettingsRequest(
        string? Provider, string? Model, string? ApiKey,
        string? OllamaHost, string? ExecMode, bool? ForceTools);

    private sealed record ChatRequest(string Prompt, string? ExecMode, bool? Verbose, string? SessionId);
    private sealed record HistoryItem(string Role, string Content);
}
