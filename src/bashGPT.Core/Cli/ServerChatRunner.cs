using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Execution;

namespace BashGPT.Cli;

/// <summary>
/// Verarbeitet Chat-Anfragen im Server-Modus. Unterstützt optionalen Tool-Call-Loop,
/// wenn der Session Tools zugewiesen sind. Shell-Funktionalität wird über den
/// Shell-Agenten oder über zugewiesene Session-Tools bereitgestellt.
/// </summary>
public class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null,
    ToolRegistry? toolRegistry = null) : IPromptHandler
{
    private const int MaxToolRounds = 5;

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
        var logs              = new List<string>();
        var totalInputTokens  = 0;
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
                    Logs:     []);
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
                    Logs:     []);
            }
        }

        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, Modell: {provider.Model}");

        var messages = new List<ChatMessage>();
        foreach (var msg in opts.History)
            messages.Add(msg);
        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools       = opts.Tools ?? [];
        var llmExchanges = new List<LlmExchangeRecord>();
        var exchangeIdx  = 0;

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
                });
            llmExchanges.Add(new LlmExchangeRecord(reqJson, resJson));
            return result;
        }

        var response = await CallOnce(opts.OnToken);
        if (response.Error is not null)
            return new ServerChatResult(response.Error, []);

        totalInputTokens  += response.Response.Usage?.InputTokens  ?? 0;
        totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;

        // Tool-Call-Loop: nur wenn Tools vorhanden und ToolRegistry verfügbar
        if (tools.Count > 0 && toolRegistry is not null)
        {
            for (var round = 0; round < MaxToolRounds; round++)
            {
                if (response.Response.ToolCalls.Count == 0) break;

                opts.OnEvent?.Invoke(new SseEvent("round_start", new { round = round + 1 }));
                messages.Add(ChatMessage.AssistantWithToolCalls(response.Response.ToolCalls, response.Response.Content));

                foreach (var call in response.Response.ToolCalls)
                {
                    string toolResult;
                    if (toolRegistry.TryGet(call.Name, out var iTool) && iTool is not null)
                    {
                        var r = await iTool.ExecuteAsync(
                            new Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson ?? "{}"), ct);
                        toolResult = r.Success ? r.Content : $"Fehler: {r.Content}";
                    }
                    else
                    {
                        toolResult = $"Fehler: Unbekanntes Tool '{call.Name}'.";
                    }
                    messages.Add(ChatMessage.ToolResult(toolResult, call.Id, call.Name));
                }

                response = await CallOnce();
                if (response.Error is not null) break;
                totalInputTokens  += response.Response.Usage?.InputTokens  ?? 0;
                totalOutputTokens += response.Response.Usage?.OutputTokens ?? 0;
            }
        }

        TokenUsage? BuildUsage() => totalInputTokens > 0 || totalOutputTokens > 0
            ? new TokenUsage(totalInputTokens, totalOutputTokens)
            : null;

        return new ServerChatResult(
            Response:     response.Response.Content,
            Logs:         logs,
            Usage:        BuildUsage(),
            LlmExchanges: llmExchanges.Count > 0 ? llmExchanges : null);
    }

    private LlmRateLimiter? GetOrCreateLimiter(AppConfig config)
    {
        if (!config.RateLimiting.Enabled) return null;
        var rpm   = config.RateLimiting.MaxRequestsPerMinute;
        var delay = config.RateLimiting.AgentRequestDelayMs;
        lock (_limiterLock)
        {
            if (_sharedLimiter is null || _limiterMaxRpm != rpm || _limiterMinIntervalMs != delay)
            {
                _sharedLimiter        = new LlmRateLimiter(rpm, delay);
                _limiterMaxRpm        = rpm;
                _limiterMinIntervalMs = delay;
            }
            return _sharedLimiter;
        }
    }
}
