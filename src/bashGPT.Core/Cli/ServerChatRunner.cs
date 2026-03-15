using BashGPT.Configuration;
using BashGPT.Providers;
using System.Text.Json;
using BashGPT.Shell;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Execution;

namespace BashGPT.Cli;

/// <summary>
/// Verarbeitet Chat-Anfragen im Server-Modus. Unterst�tzt optionalen Tool-Call-Loop,
/// wenn der Session Tools zugewiesen sind. Shell-Funktionalit�t wird �ber den
/// Shell-Agenten oder �ber zugewiesene Session-Tools bereitgestellt.
/// </summary>
public class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null,
    ToolRegistry? toolRegistry = null) : IPromptHandler
{
    // Shared across all requests so the rate limit is truly global per process.
    // Recreated automatically when the rate-limiting config values change.
    private LlmRateLimiter? _sharedLimiter;
    private int _limiterMaxRpm;
    private int _limiterMinIntervalMs;
    private readonly object _limiterLock = new();

    public async Task<ServerChatResult> RunServerChatAsync(
        ServerChatOptions opts,
        CancellationToken ct = default)
    {
        var logs = new List<string>();
        var totalInputTokens = 0;
        var totalOutputTokens = 0;

        ILlmProvider provider;
        if (providerOverride is not null)
        {
            provider = providerOverride;
        }
        else
        {
            AppConfig config;
            try
            {
                config = await configService.LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                return new ServerChatResult(
                    Response: $"Konfigurationsfehler: {ex.Message}",
                    Logs: []);
            }

            ChatOrchestrator.ApplyModelOverride(config, opts.Provider, opts.Model);

            try
            {
                provider = ProviderFactory.Create(config, opts.Provider, GetOrCreateLimiter(config));
            }
            catch (Exception ex)
            {
                return new ServerChatResult(
                    Response: $"Provider-Fehler: {ex.Message}",
                    Logs: []);
            }
        }

        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, Modell: {provider.Model}");

        var messages = new List<ChatMessage>();
        if (opts.SystemPrompt is not null)
            foreach (var prompt in opts.SystemPrompt.Where(p => !string.IsNullOrWhiteSpace(p)))
                messages.Add(new ChatMessage(ChatRole.System, prompt));
        foreach (var msg in opts.History)
            messages.Add(msg);
        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools = opts.Tools ?? [];
        var llmExchanges = new List<LlmExchangeRecord>();
        var exchangeIdx = 0;
        var commandResults = new List<CommandResult>();
        var usedToolCalls = false;
        var conversationDelta = new List<ChatMessage>();
        LlmChatResponse? lastResponse = null;

        async Task<(LlmChatResponse Response, string? Error)> CallOnce(Action<string>? onToken = null)
        {
            var idx = exchangeIdx++;
            string? reqJson = null;
            string? resJson = null;
            var result = await ChatOrchestrator.ChatOnceAsync(
                provider, messages, tools, null, ct, onToken,
                onReasoningToken: opts.OnReasoningToken,
                onRequestJson: async json =>
                {
                    reqJson = json;
                    if (opts.OnLlmRequestJson is not null) await opts.OnLlmRequestJson(idx, json);
                },
                onResponseJson: async json =>
                {
                    resJson = json;
                    if (opts.OnLlmResponseJson is not null) await opts.OnLlmResponseJson(idx, json);
                },
                llmConfig: opts.LlmConfig);
            llmExchanges.Add(new LlmExchangeRecord(reqJson, resJson));
            return result;
        }

        try
        {
            var response = await CallOnce(opts.OnToken);
            if (response.Error is not null)
                return new ServerChatResult(response.Error, []);

            lastResponse = response.Response;
            totalInputTokens += response.Response.Usage?.InputTokens ?? 0;
            totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;

            // Tool-Call-Loop: nur wenn Tools vorhanden und ToolRegistry verf�gbar
            if (tools.Count > 0 && toolRegistry is not null)
            {
                var round = 0;
                while (!ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    if (response.Response.ToolCalls.Count == 0) break;
                    usedToolCalls = true;

                    round++;
                    opts.OnEvent?.Invoke(new SseEvent("round_start", new { round }));
                    var assistantToolCallMessage = ChatMessage.AssistantWithToolCalls(
                        response.Response.ToolCalls,
                        content: response.Response.Content);
                    messages.Add(assistantToolCallMessage);
                    conversationDelta.Add(assistantToolCallMessage);

                    foreach (var call in response.Response.ToolCalls)
                    {
                        ct.ThrowIfCancellationRequested();

                        var commandLabel = TryExtractCommand(call.ArgumentsJson, out var parsedCommand)
                            ? parsedCommand
                            : call.Name;
                        opts.OnEvent?.Invoke(new SseEvent("tool_call", new { name = call.Name, command = commandLabel }));

                        CommandResult commandResult;
                        string toolResult;
                        if (toolRegistry.TryGet(call.Name, out var iTool) && iTool is not null)
                        {
                            try
                            {
                                var r = await iTool.ExecuteAsync(
                                    new Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson ?? "{}", opts.SessionPath), ct);

                                if (r.InjectAsSystem && r.Success)
                                {
                                    // System-Message nach den bestehenden System-Prompts einfügen
                                    // (konsistent mit BuildLoadedFilesContext, das ebenfalls als letzter
                                    // System-Prompt-Eintrag landet). Außerdem in conversationDelta speichern,
                                    // damit sie in der Session persistiert wird.
                                    var injected = new ChatMessage(ChatRole.System, r.Content);
                                    var lastSystemIdx = messages.FindLastIndex(m => m.Role == ChatRole.System);
                                    messages.Insert(lastSystemIdx >= 0 ? lastSystemIdx + 1 : 0, injected);
                                    conversationDelta.Add(injected);
                                    toolResult = "Dateien erfolgreich in den System-Kontext geladen.";
                                }
                                else
                                {
                                    toolResult = r.Content;
                                }
                                commandResult = BuildCommandResult(call.Name, commandLabel, r.Content, r.Success);
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                toolResult = $"Fehler: {ex.Message}";
                                commandResult = new CommandResult(commandLabel, 1, toolResult, WasExecuted: false);
                            }
                        }
                        else
                        {
                            toolResult = $"Fehler: Unbekanntes Tool '{call.Name}'.";
                            commandResult = new CommandResult(commandLabel, 1, toolResult, WasExecuted: false);
                        }

                        commandResults.Add(commandResult);
                        opts.OnEvent?.Invoke(new SseEvent("command_result", new
                        {
                            command = commandResult.Command,
                            output = commandResult.Output,
                            exitCode = commandResult.ExitCode,
                            wasExecuted = commandResult.WasExecuted,
                            status = ClassifyCommandStatus(commandResult),
                        }));
                        var toolMessage = ChatMessage.ToolResult(toolResult, call.Id, call.Name);
                        messages.Add(toolMessage);
                        conversationDelta.Add(toolMessage);
                    }

                    response = await CallOnce();
                    if (response.Error is not null) break;
                    lastResponse = response.Response;
                    totalInputTokens += response.Response.Usage?.InputTokens ?? 0;
                    totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var cancelledText = "Vom Nutzer abgebrochen.";
            var cancelledAssistant = new ChatMessage(ChatRole.Assistant, cancelledText);
            conversationDelta.Add(cancelledAssistant);

            TokenUsage? cancelledUsage = totalInputTokens > 0 || totalOutputTokens > 0
                ? new TokenUsage(totalInputTokens, totalOutputTokens)
                : null;

            return new ServerChatResult(
                Response: cancelledText,
                Logs: logs,
                Usage: cancelledUsage,
                LlmExchanges: llmExchanges.Count > 0 ? llmExchanges : null,
                Commands: commandResults.Count > 0 ? commandResults : null,
                UsedToolCalls: usedToolCalls,
                ConversationDelta: conversationDelta,
                FinalStatus: "user_cancelled");
        }

        TokenUsage? BuildUsage() => totalInputTokens > 0 || totalOutputTokens > 0
            ? new TokenUsage(totalInputTokens, totalOutputTokens)
            : null;

        var finalResponse = lastResponse?.Content ?? string.Empty;
        var finalAssistantMessage = new ChatMessage(ChatRole.Assistant, finalResponse);
        conversationDelta.Add(finalAssistantMessage);

        var finalStatus = commandResults.Any(r => string.Equals(ClassifyCommandStatus(r), "timeout", StringComparison.Ordinal))
            ? "timeout"
            : "completed";

        return new ServerChatResult(
            Response: finalResponse,
            Logs: logs,
            Usage: BuildUsage(),
            LlmExchanges: llmExchanges.Count > 0 ? llmExchanges : null,
            Commands: commandResults.Count > 0 ? commandResults : null,
            UsedToolCalls: usedToolCalls,
            ConversationDelta: conversationDelta,
            FinalStatus: finalStatus);
    }

    private LlmRateLimiter? GetOrCreateLimiter(AppConfig config)
    {
        if (!config.RateLimiting.Enabled) return null;
        var rpm = config.RateLimiting.MaxRequestsPerMinute;
        var delay = config.RateLimiting.AgentRequestDelayMs;
        lock (_limiterLock)
        {
            if (_sharedLimiter is null || _limiterMaxRpm != rpm || _limiterMinIntervalMs != delay)
            {
                _sharedLimiter = new LlmRateLimiter(rpm, delay);
                _limiterMaxRpm = rpm;
                _limiterMinIntervalMs = delay;
            }
            return _sharedLimiter;
        }
    }

    private static bool TryExtractCommand(string? argumentsJson, out string command)
    {
        command = string.Empty;
        if (string.IsNullOrWhiteSpace(argumentsJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (!doc.RootElement.TryGetProperty("command", out var commandEl)) return false;
            var value = commandEl.GetString();
            if (string.IsNullOrWhiteSpace(value)) return false;
            command = value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static CommandResult BuildCommandResult(string toolName, string command, string toolResult, bool success)
    {
        if (string.Equals(toolName, "shell_exec", StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseShellExecOutput(toolResult, out var output, out var exitCode))
            {
                return new CommandResult(command, exitCode, output, WasExecuted: true);
            }

            return new CommandResult(command, success ? 0 : 1, toolResult, WasExecuted: success);
        }

        return new CommandResult(command, success ? 0 : 1, toolResult, WasExecuted: success);
    }

    private static bool TryParseShellExecOutput(string content, out string output, out int exitCode)
    {
        output = content;
        exitCode = 1;
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("exitCode", out var exitCodeEl)) return false;

            var stdout = root.TryGetProperty("stdout", out var stdoutEl) ? (stdoutEl.GetString() ?? string.Empty) : string.Empty;
            var stderr = root.TryGetProperty("stderr", out var stderrEl) ? (stderrEl.GetString() ?? string.Empty) : string.Empty;
            exitCode = exitCodeEl.GetInt32();
            output = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}\n{stderr}".Trim();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ClassifyCommandStatus(CommandResult result)
    {
        if (!result.WasExecuted) return "skipped";

        var output = result.Output ?? string.Empty;
        if (output.Contains("\"timedOut\":true", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "timeout";
        }

        return result.ExitCode == 0 ? "success" : "error";
    }
}
