using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;
using System.Reflection;

namespace BashGPT.Cli.Tests;

/// <summary>
/// Unit-Tests für ServerChatRunner.RunServerChatAsync.
/// Nutzt FakeLlmProvider (via providerOverride) und NoContext=true, um echte
/// LLM- und Dateisystem-Aufrufe zu vermeiden.
/// </summary>
public sealed class ServerChatRunnerTests
{
    // ── Hilfsmethoden ───────────────────────────────────────────────────────

    private static ServerChatRunner CreateRunner(FakeLlmProvider provider) =>
        new(new ConfigurationService(), new ShellContextCollector(), provider);

    private static ServerChatOptions Opts(
        string prompt = "Hallo",
        ExecutionMode execMode = ExecutionMode.NoExec,
        bool verbose = false,
        bool forceTools = false,
        bool noContext = true,
        IReadOnlyList<ChatMessage>? history = null) =>
        new(
            Prompt:     prompt,
            History:    history ?? [],
            Provider:   null,
            Model:      null,
            NoContext:  noContext,
            IncludeDir: false,
            ExecMode:   execMode,
            Verbose:    verbose,
            ForceTools: forceTools);

    private static ToolCall BashCall(string cmd, string id = "tc-1") =>
        new(id, "bash", $$"""{"command":"{{cmd}}"}""");

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunServerChatAsync_SimpleText_ReturnsContent()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Hallo Welt!", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Equal("Hallo Welt!", result.Response);
        Assert.False(result.UsedToolCalls);
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
    public async Task RunServerChatAsync_SingleToolCall_UsedToolCallsTrue()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("echo hi")]));
        provider.Enqueue(new LlmChatResponse("Fertig!", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.True(result.UsedToolCalls);
        Assert.Equal(2, provider.CallCount);
        Assert.Equal("Fertig!", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_ThreeToolCallRounds_FourProviderCalls()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("pwd", "tc-2")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("date", "tc-3")]));
        provider.Enqueue(new LlmChatResponse("Alle Runden erledigt.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal(4, provider.CallCount);
        Assert.True(result.UsedToolCalls);
        Assert.Equal("Alle Runden erledigt.", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_IdenticalConsecutiveToolCalls_ReturnsLoopDetectedMessage()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("top", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("top", "tc-2")]));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Contains("Schleife", result.Response);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_LoopGuardWithExistingContent_ReturnsContent()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("top", "tc-1")]));
        // 2. Antwort hat Tool-Calls UND Content → Content wird zurückgegeben
        provider.Enqueue(new LlmChatResponse("Eigene Antwort trotz Tool-Call.", [BashCall("top", "tc-2")]));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal("Eigene Antwort trotz Tool-Call.", result.Response);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_MaxRoundsReachedWithDifferentCalls_ReturnsMaxRoundsMessage()
    {
        // 9 Antworten mit je verschiedenen Befehlen → nach 8 Runden kein Loop, aber MaxRounds erreicht
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd1", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd2", "tc-2")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd3", "tc-3")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd4", "tc-4")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd5", "tc-5")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd6", "tc-6")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd7", "tc-7")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd8", "tc-8")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("cmd9", "tc-9")]));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.DoesNotContain("Schleife", result.Response);
        Assert.Contains("Maximale Anzahl", result.Response);
        Assert.Equal(9, provider.CallCount);
        Assert.True(result.UsedToolCalls);
    }

    [Fact]
    public async Task RunServerChatAsync_DifferentToolCallsEachRound_NoLoopDetected()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls",   "tc-1")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("pwd",  "tc-2")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("date", "tc-3")]));
        provider.Enqueue(new LlmChatResponse("Fertig!", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal("Fertig!", result.Response);
        Assert.DoesNotContain("Schleife", result.Response);
        Assert.Equal(4, provider.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_AskExecMode_CommandsNotExecuted()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("rm -rf /", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("Erledigt.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.Ask));

        Assert.True(result.Commands.All(c => !c.WasExecuted));
    }

    [Fact]
    public async Task RunServerChatAsync_DryRunExecMode_CommandsNotExecuted()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls -la", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("Erledigt.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.True(result.Commands.All(c => !c.WasExecuted));
        Assert.NotEmpty(result.Commands);
    }

    [Fact]
    public async Task RunServerChatAsync_NoExecMode_FallbackCommandsNotCollected()
    {
        // Text-Antwort mit Bash-Block → NoExec überspringt Ausführung
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("```bash\nls\n```", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.NoExec));

        Assert.Empty(result.Commands);
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
    public async Task RunServerChatAsync_MultiRound_TokenUsageAccumulated()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls", "tc-1")], new TokenUsage(10, 5)));
        provider.Enqueue(new LlmChatResponse("Fertig.", [], new TokenUsage(20, 8)));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.NotNull(result.Usage);
        Assert.Equal(30, result.Usage!.InputTokens);
        Assert.Equal(13, result.Usage.OutputTokens);
    }

    [Fact]
    public async Task RunServerChatAsync_NoTokenUsage_UsageIsNull()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort", []));  // Usage == null
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Null(result.Usage);
    }

    [Fact]
    public async Task RunServerChatAsync_MalformedJsonToolCall_CompletesWithoutException()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [new ToolCall("tc-1", "bash", "nicht-json")]));
        provider.Enqueue(new LlmChatResponse("Fehler ignoriert.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal("Fehler ignoriert.", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_UnknownToolName_CompletesWithoutException()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [new ToolCall("tc-1", "unbekannt", """{"command":"ls"}""")]));
        provider.Enqueue(new LlmChatResponse("Unbekanntes Tool ignoriert.", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal("Unbekanntes Tool ignoriert.", result.Response);
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
    public async Task RunServerChatAsync_WithoutProviderOverride_UsesConfigProvider_AndCollectsContext()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-cli-tests-{Guid.NewGuid()}.json");
        try
        {
            var configService = new TestConfigurationService(configPath);
            var config = await configService.LoadAsync();
            config.DefaultProvider = ProviderType.Cerebras;
            config.Cerebras.ApiKey = null;
            await configService.SaveAsync(config);

            var sut = new ServerChatRunner(configService, new ShellContextCollector());
            var result = await sut.RunServerChatAsync(Opts(noContext: false, verbose: true));

            Assert.Contains("Kein Cerebras API-Key konfiguriert", result.Response);
            Assert.Contains(result.Logs, l => l.StartsWith("Kontext gesammelt:", StringComparison.Ordinal));
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
            var sut = new ServerChatRunner(configService, new ShellContextCollector());

            var result = await sut.RunServerChatAsync(Opts());

            Assert.Contains("Konfigurationsfehler:", result.Response);
            Assert.False(result.UsedToolCalls);
            Assert.Empty(result.Commands);
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
            var sut = new ServerChatRunner(configService, new ShellContextCollector());

            var result = await sut.RunServerChatAsync(Opts());

            Assert.Contains("Provider-Fehler:", result.Response);
            Assert.False(result.UsedToolCalls);
            Assert.Empty(result.Commands);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task RunServerChatAsync_VerboseToolRound_LogsParsedToolCommand()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("echo hi", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("fertig", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun, verbose: true));

        Assert.Contains(result.Logs, l => l.Contains("Tool 'bash' -> echo hi", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunServerChatAsync_ToolRoundNextResponseError_ReturnsErrorAndKeepsCommands()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("echo hi", "tc-1")]));
        provider.ExceptionForCall = call => call == 2 ? new LlmProviderException("kaputt") : null;
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.DryRun));

        Assert.Equal("Fehler: kaputt", result.Response);
        Assert.True(result.UsedToolCalls);
        Assert.NotEmpty(result.Commands);
    }

    [Fact]
    public async Task RunServerChatAsync_FallbackWithAskExecMode_UsesDryRun()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("```bash\necho hi\n```", []));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.Ask));

        Assert.Single(result.Commands);
        Assert.False(result.Commands[0].WasExecuted);
    }

    [Fact]
    public async Task RunServerChatAsync_FallbackFollowUpError_ReturnsError()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("```bash\necho hi\n```", []));
        provider.ExceptionForCall = call => call == 2 ? new LlmProviderException("follow-up failed") : null;
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.AutoExec));

        Assert.Equal("Fehler: follow-up failed", result.Response);
        Assert.Single(result.Commands);
        Assert.True(result.Commands[0].WasExecuted);
    }

    [Fact]
    public async Task RunServerChatAsync_FallbackFollowUpSuccess_AccumulatesUsageAndReturnsFollowUpContent()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("```bash\necho hi\n```", [], new TokenUsage(3, 2)));
        provider.Enqueue(new LlmChatResponse("Finale Antwort", [], new TokenUsage(5, 4)));
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts(execMode: ExecutionMode.AutoExec));

        Assert.Equal("Finale Antwort", result.Response);
        Assert.Single(result.Commands);
        Assert.True(result.Commands[0].WasExecuted);
        Assert.NotNull(result.Usage);
        Assert.Equal(8, result.Usage!.InputTokens);
        Assert.Equal(6, result.Usage.OutputTokens);
    }

    [Fact]
    public void GetOrCreateLimiter_ReusesAndRecreates_ByConfig()
    {
        var sut = new ServerChatRunner(new ConfigurationService(), new ShellContextCollector());
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

        var config3 = new AppConfig();
        config3.RateLimiting.Enabled = true;
        config3.RateLimiting.MaxRequestsPerMinute = 11;
        config3.RateLimiting.AgentRequestDelayMs = 101;

        var limiter4 = method.Invoke(sut, [config3]);
        Assert.NotSame(limiter3, limiter4);
    }

    [Fact]
    public void GetOrCreateLimiter_Disabled_ReturnsNull()
    {
        var sut = new ServerChatRunner(new ConfigurationService(), new ShellContextCollector());
        var method = typeof(ServerChatRunner).GetMethod("GetOrCreateLimiter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var config = new AppConfig();
        config.RateLimiting.Enabled = false;

        var limiter = method!.Invoke(sut, [config]);

        Assert.Null(limiter);
    }
}
