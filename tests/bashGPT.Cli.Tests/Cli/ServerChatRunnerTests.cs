using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using System.Reflection;

namespace BashGPT.Cli.Tests;

/// <summary>
/// Unit-Tests für ServerChatRunner.RunServerChatAsync.
/// Nutzt FakeLlmProvider (via providerOverride) – reines LLM-Chat ohne Tools.
/// </summary>
public sealed class ServerChatRunnerTests
{
    // ── Hilfsmethoden ───────────────────────────────────────────────────────

    private static ServerChatRunner CreateRunner(FakeLlmProvider provider) =>
        new(new ConfigurationService(), provider);

    private static ServerChatOptions Opts(
        string prompt = "Hallo",
        bool verbose = false,
        IReadOnlyList<ChatMessage>? history = null) =>
        new(
            Prompt:   prompt,
            History:  history ?? [],
            Provider: null,
            Model:    null,
            Verbose:  verbose);

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunServerChatAsync_SimpleText_ReturnsContent()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Hallo Welt!", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Equal("Hallo Welt!", result.Response);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_LlmProviderException_ReturnsErrorMessage()
    {
        var provider = new FakeLlmProvider();
        provider.NextException = new LlmProviderException("API-Fehler");
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Contains("Fehler:", result.Response);
        Assert.Contains("API-Fehler", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_OperationCanceled_ReturnsAbortedMessage()
    {
        var provider = new FakeLlmProvider();
        provider.NextException = new OperationCanceledException();
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Equal("Abgebrochen.", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_SingleRound_TokenUsageAccumulated()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort", [], new TokenUsage(10, 5)));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage!.InputTokens);
        Assert.Equal(5,  result.Usage.OutputTokens);
    }

    [Fact]
    public async Task RunServerChatAsync_NoTokenUsage_UsageIsNull()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Null(result.Usage);
    }

    [Fact]
    public async Task RunServerChatAsync_Verbose_LogsContainProviderInfo()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Ok.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(verbose: true));

        Assert.Contains(result.Logs, l => l.Contains("fake") && l.Contains("fake-model"));
    }

    [Fact]
    public async Task RunServerChatAsync_History_ForwardedToProvider()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.User,      "Frühere Frage"),
            new(ChatRole.Assistant, "Frühere Antwort"),
        };
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort.", []));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(Opts(history: history));

        Assert.NotNull(provider.LastRequestMessages);
        Assert.Contains(provider.LastRequestMessages!, m =>
            m.Role == ChatRole.User && m.Content == "Frühere Frage");
        Assert.Contains(provider.LastRequestMessages!, m =>
            m.Role == ChatRole.Assistant && m.Content == "Frühere Antwort");
    }

    [Fact]
    public async Task RunServerChatAsync_WithoutProviderOverride_UsesConfigProvider()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-cli-tests-{Guid.NewGuid()}.json");
        try
        {
            var configService = new TestConfigurationService(configPath);
            var config = await configService.LoadAsync();
            config.DefaultProvider = ProviderType.Cerebras;
            config.Cerebras.ApiKey = null;
            await configService.SaveAsync(config);

            var sut = new ServerChatRunner(configService);
            var result = await sut.RunServerChatAsync(Opts());

            Assert.Contains("Kein Cerebras API-Key konfiguriert", result.Response);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task RunServerChatAsync_WhenConfigLoadFails_ReturnsConfigurationError()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-cli-tests-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, "{ invalid-json ");
            var configService = new TestConfigurationService(configPath);
            var sut = new ServerChatRunner(configService);

            var result = await sut.RunServerChatAsync(Opts());

            Assert.Contains("Konfigurationsfehler:", result.Response);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task RunServerChatAsync_WhenProviderFactoryThrows_ReturnsProviderError()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-cli-tests-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, """
            {
              "defaultProvider": 999
            }
            """);
            var configService = new TestConfigurationService(configPath);
            var sut = new ServerChatRunner(configService);

            var result = await sut.RunServerChatAsync(Opts());

            Assert.Contains("Provider-Fehler:", result.Response);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    // ── LlmExchanges-Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task RunServerChatAsync_SimpleResponse_OneLlmExchange()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.NotNull(result.LlmExchanges);
        Assert.Single(result.LlmExchanges!);
        Assert.NotNull(result.LlmExchanges[0].RequestJson);
        Assert.NotNull(result.LlmExchanges[0].ResponseJson);
    }

    // ── Rate-Limiter-Tests ───────────────────────────────────────────────────

    [Fact]
    public void GetOrCreateLimiter_ReusesAndRecreates_ByConfig()
    {
        var sut = new ServerChatRunner(new ConfigurationService());
        var method = typeof(ServerChatRunner).GetMethod("GetOrCreateLimiter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var config1 = new AppConfig();
        config1.RateLimiting.Enabled = true;
        config1.RateLimiting.MaxRequestsPerMinute = 10;
        config1.RateLimiting.AgentRequestDelayMs = 100;

        var limiter1 = method!.Invoke(sut, [config1]);
        var limiter2 = method.Invoke(sut, [config1]);
        Assert.Same(limiter1, limiter2);

        var config2 = new AppConfig();
        config2.RateLimiting.Enabled = true;
        config2.RateLimiting.MaxRequestsPerMinute = 11;
        config2.RateLimiting.AgentRequestDelayMs = 100;

        var limiter3 = method.Invoke(sut, [config2]);
        Assert.NotSame(limiter1, limiter3);
    }

    [Fact]
    public void GetOrCreateLimiter_Disabled_ReturnsNull()
    {
        var sut = new ServerChatRunner(new ConfigurationService());
        var method = typeof(ServerChatRunner).GetMethod("GetOrCreateLimiter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var config = new AppConfig();
        config.RateLimiting.Enabled = false;

        var limiter = method!.Invoke(sut, [config]);

        Assert.Null(limiter);
    }
}
