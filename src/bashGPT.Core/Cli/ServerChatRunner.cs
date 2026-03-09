using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Cli;

/// <summary>
/// Verarbeitet Chat-Anfragen im Server-Modus: reines LLM-Chat ohne Shell-Kontext oder Tools.
/// Shell-Funktionalität wird über den Shell-Agenten bereitgestellt.
/// </summary>
public class ServerChatRunner(
    ConfigurationService configService,
    ILlmProvider? providerOverride = null) : IPromptHandler
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

        var llmExchanges = new List<LlmExchangeRecord>();
        var exchangeIdx  = 0;

        async Task<(LlmChatResponse Response, string? Error)> CallOnce(Action<string>? onToken = null)
        {
            var idx = exchangeIdx++;
            string? reqJson = null;
            string? resJson = null;
            var result = await ChatOrchestrator.ChatOnceAsync(
                provider, messages, [], null, ct, onToken,
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
