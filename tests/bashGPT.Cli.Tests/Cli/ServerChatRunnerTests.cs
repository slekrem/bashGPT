using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Tests.Cli;

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
        IReadOnlyList<ChatMessage>? history = null) =>
        new(
            Prompt:     prompt,
            History:    history ?? [],
            Provider:   null,
            Model:      null,
            NoContext:  true,
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
}
