using System.Diagnostics;
using System.Net;
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
                await WriteHtmlAsync(ctx.Response, Html);
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

    private static async Task WriteHtmlAsync(HttpListenerResponse response, string html)
    {
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
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

    private const string Html = """
<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>bashGPT Server</title>
  <style>
    :root { --bg:#0f172a; --panel:#111827; --line:#374151; --text:#e5e7eb; --muted:#9ca3af; --accent:#22c55e; --user:#1f2937; --assistant:#0b1220; }
    body { margin:0; font-family: ui-sans-serif, system-ui, sans-serif; background: radial-gradient(circle at top, #1e293b, #020617); color: var(--text); }
    .wrap { max-width: 1000px; margin: 0 auto; padding: 20px; }
    .head { display:flex; justify-content:space-between; gap:12px; align-items:center; margin-bottom:16px; }
    .title { font-size: 22px; font-weight: 700; }
    .muted { color: var(--muted); font-size: 13px; }
    #chat { border:1px solid var(--line); border-radius:12px; min-height: 420px; background: rgba(15,23,42,0.7); padding: 14px; overflow:auto; }
    .msg { border:1px solid var(--line); border-radius:10px; padding:10px; margin-bottom:10px; white-space:pre-wrap; }
    .msg.user { background: var(--user); }
    .msg.assistant { background: var(--assistant); }
    .meta { font-size:12px; color: var(--muted); margin-bottom:6px; }
    .cmd { border:1px solid #475569; border-radius:8px; padding:8px; margin:8px 0; background:#0b1220; }
    .cmd code { color:#93c5fd; }
    .row { display:flex; gap:10px; margin-top:12px; }
    textarea { flex:1; min-height:84px; max-height:240px; resize:vertical; background:var(--panel); color:var(--text); border:1px solid var(--line); border-radius:10px; padding:10px; }
    select, button { background:var(--panel); color:var(--text); border:1px solid var(--line); border-radius:10px; padding:10px 12px; }
    button.primary { background:#14532d; border-color:#16a34a; }
    .status { margin-top:8px; font-size:13px; color:var(--muted); }
  </style>
</head>
<body>
  <div class="wrap">
    <div class="head">
      <div>
        <div class="title">bashGPT Server UI</div>
        <div class="muted">Lokale Session mit Verlauf. Exec-Mode pro Nachricht wählbar.</div>
      </div>
      <div>
        <button id="reset">Verlauf löschen</button>
      </div>
    </div>
    <div id="chat"></div>
    <div class="row">
      <textarea id="prompt" placeholder="Nachricht eingeben..."></textarea>
    </div>
    <div class="row">
      <select id="mode">
        <option value="ask">ask</option>
        <option value="dry-run">dry-run</option>
        <option value="auto-exec">auto-exec</option>
        <option value="no-exec">no-exec</option>
      </select>
      <button id="send" class="primary">Senden</button>
    </div>
    <div id="status" class="status"></div>
  </div>
  <script>
    const chat = document.getElementById('chat');
    const promptEl = document.getElementById('prompt');
    const modeEl = document.getElementById('mode');
    const sendBtn = document.getElementById('send');
    const resetBtn = document.getElementById('reset');
    const statusEl = document.getElementById('status');

    function escapeHtml(s) {
      return s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
    }

    function addMessage(role, content) {
      const div = document.createElement('div');
      div.className = `msg ${role === 'user' ? 'user' : 'assistant'}`;
      div.innerHTML = `<div class="meta">${role}</div><div>${escapeHtml(content)}</div>`;
      chat.appendChild(div);
      chat.scrollTop = chat.scrollHeight;
      return div;
    }

    function addCommands(parent, commands) {
      if (!commands || commands.length === 0) return;
      for (const c of commands) {
        const div = document.createElement('div');
        div.className = 'cmd';
        div.innerHTML =
          `<div><code>${escapeHtml(c.command)}</code></div>` +
          `<div>Executed: ${c.wasExecuted ? 'yes' : 'no'} | Exit: ${c.exitCode}</div>` +
          `<div>${escapeHtml(c.output || '(keine Ausgabe)')}</div>`;
        parent.appendChild(div);
      }
    }

    async function loadHistory() {
      const res = await fetch('/api/history');
      const data = await res.json();
      chat.innerHTML = '';
      for (const h of data.history || [])
        addMessage(h.role, h.content);
    }

    async function send() {
      const prompt = promptEl.value.trim();
      if (!prompt) return;

      sendBtn.disabled = true;
      statusEl.textContent = 'Sende...';
      addMessage('user', prompt);
      promptEl.value = '';

      try {
        const res = await fetch('/api/chat', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ prompt, execMode: modeEl.value })
        });
        const data = await res.json();

        if (!res.ok) {
          addMessage('assistant', data.error || 'Unbekannter Fehler');
        } else {
          const msg = addMessage('assistant', data.response || '');
          addCommands(msg, data.commands || []);
          const logCount = (data.logs || []).length;
          statusEl.textContent = `Fertig. tool_calls=${data.usedToolCalls ? 'yes' : 'no'} logs=${logCount}`;
        }
      } catch (e) {
        addMessage('assistant', String(e));
        statusEl.textContent = 'Fehler.';
      } finally {
        sendBtn.disabled = false;
      }
    }

    sendBtn.addEventListener('click', send);
    promptEl.addEventListener('keydown', e => {
      if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) send();
    });
    resetBtn.addEventListener('click', async () => {
      await fetch('/api/reset', { method: 'POST' });
      await loadHistory();
      statusEl.textContent = 'Verlauf gelöscht.';
    });

    loadHistory();
  </script>
</body>
</html>
""";
}
