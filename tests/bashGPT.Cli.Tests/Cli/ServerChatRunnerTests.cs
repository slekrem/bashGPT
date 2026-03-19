using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Tools.Execution;
using BashGPT.Server;

namespace BashGPT.Cli.Tests;

/// <summary>
/// Unit-Tests fÃ¼r ServerChatRunner.RunServerChatAsync.
/// Nutzt FakeLlmProvider (via providerOverride) â€“ reines LLM-Chat ohne Tools.
/// </summary>
public sealed class ServerChatRunnerTests
{
    private static ServerChatRunner CreateRunner(FakeLlmProvider provider) =>
        new(new ConfigurationService(), provider);

    private static ServerChatOptions Opts(
        string prompt = "Hallo",
        bool verbose = false,
        IReadOnlyList<ChatMessage>? history = null) =>
        new(
            Prompt:   prompt,
            History:  history ?? [],
            Model:    null,
            Verbose:  verbose);

    // â”€â”€ Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

        Assert.Contains("Error:", result.Response);
        Assert.Contains("API-Fehler", result.Response);
    }

    [Fact]
    public async Task RunServerChatAsync_OperationCanceled_ReturnsAbortedMessage()
    {
        var provider = new FakeLlmProvider();
        provider.NextException = new OperationCanceledException();
        var sut = CreateRunner(provider);

        var result = await sut.RunServerChatAsync(Opts());

        Assert.Equal("Cancelled.", result.Response);
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
            new(ChatRole.User,      "FrÃ¼here Frage"),
            new(ChatRole.Assistant, "FrÃ¼here Antwort"),
        };
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort.", []));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(Opts(history: history));

        Assert.NotNull(provider.LastRequestMessages);
        Assert.Contains(provider.LastRequestMessages!, m =>
            m.Role == ChatRole.User && m.Content == "FrÃ¼here Frage");
        Assert.Contains(provider.LastRequestMessages!, m =>
            m.Role == ChatRole.Assistant && m.Content == "FrÃ¼here Antwort");
    }

    [Fact]
    public async Task RunServerChatAsync_WithLegacyProviderFieldInConfig_IgnoresIt()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-cli-tests-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, """
            {
              "defaultProvider": "legacy-provider"
            }
            """);

            var configService = new TestConfigurationService(configPath);
            var sut = new ServerChatRunner(configService);
            var result = await sut.RunServerChatAsync(Opts());

            Assert.NotEqual(string.Empty, result.Response);
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

            Assert.Contains("Configuration error:", result.Response);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task RunServerChatAsync_WithToolsAndNoToolCallsInResponse_SingleLlmCall()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Antwort ohne Tools.", []));

        var fakeTool = new FakeTool("my_tool");
        var registry = new ToolRegistry([fakeTool]);
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Hallo",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Antwort ohne Tools.", result.Response);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal(0, fakeTool.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_WithToolsAndToolCall_ExecutesToolAndCallsLlmAgain()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse(
            "Ich rufe das Tool auf.",
            [new Providers.ToolCall("call-1", "my_tool", "{\"input\":\"x\"}")]));
        provider.Enqueue(new LlmChatResponse("Fertig nach Tool.", []));

        var fakeTool = new FakeTool("my_tool", returnValue: "Tool-Ausgabe");
        var registry = new ToolRegistry([fakeTool]);
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Benutze das Tool",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Fertig nach Tool.", result.Response);
        Assert.Equal(2, provider.CallCount);
        Assert.Equal(1, fakeTool.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_WithToolCalls_ForwardsAssistantContentWithToolCalls()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse(
            "Planungs- und Reasoning-Text, der in Runde 2 mit den Tool-Calls enthalten sein soll.",
            [new Providers.ToolCall("call-1", "my_tool", "{}")]));
        provider.Enqueue(new LlmChatResponse("Finale Antwort.", []));

        var fakeTool = new FakeTool("my_tool", returnValue: "ok");
        var registry = new ToolRegistry([fakeTool]);
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Nutze ein Tool",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Finale Antwort.", result.Response);
        Assert.NotNull(provider.LastRequestMessages);

        var assistantToolCallMessage = provider.LastRequestMessages!
            .FirstOrDefault(m => m.Role == ChatRole.Assistant && m.ToolCalls is { Count: > 0 });
        Assert.NotNull(assistantToolCallMessage);
        Assert.Equal("Planungs- und Reasoning-Text, der in Runde 2 mit den Tool-Calls enthalten sein soll.", assistantToolCallMessage!.Content);
    }

    [Fact]
    public async Task RunServerChatAsync_WithTools_UnknownTool_ReturnsErrorInToolResult()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse(
            "Ich rufe unbekanntes Tool auf.",
            [new Providers.ToolCall("call-1", "unbekannt", "{}")]));
        provider.Enqueue(new LlmChatResponse("Fehler verarbeitet.", []));

        var registry = new ToolRegistry();
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("unbekannt", "Unbekannt", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Benutze unbekanntes Tool",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Fehler verarbeitet.", result.Response);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_WithTools_NoHardRoundLimit_ProcessesUntilNoToolCalls()
    {
        // Mehrere Tool-Calls in Folge â†’ Verarbeitung bis die finale Antwort ohne Tool-Calls kommt
        var provider = new FakeLlmProvider();
        for (var i = 0; i < 10; i++)
        {
            provider.Enqueue(new LlmChatResponse(
                $"Runde {i}",
                [new Providers.ToolCall($"c{i}", "my_tool", "{}")]));
        }
        provider.Enqueue(new LlmChatResponse("Nach Loop.", []));

        var fakeTool = new FakeTool("my_tool");
        var registry = new ToolRegistry([fakeTool]);
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Schleife",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Nach Loop.", result.Response);
        Assert.Equal(11, provider.CallCount);
        Assert.Equal(10, fakeTool.CallCount);
    }

    [Fact]
    public async Task RunServerChatAsync_WithoutToolRegistry_ToolCallsNotExecuted()
    {
        // Kein Registry â†’ Loop wird nie gestartet, auch wenn Tools in opts stehen
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse(
            "Antwort mit Tool-Call.",
            [new Providers.ToolCall("c1", "my_tool", "{}")]));

        var sut = new ServerChatRunner(new ConfigurationService(), provider); // kein Registry

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Hallo",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Antwort mit Tool-Call.", result.Response);
        Assert.Equal(1, provider.CallCount); // nur ein Aufruf
    }

    [Fact]
    public async Task RunServerChatAsync_WithTools_ToolThrows_PreservesToolCallFlow()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse(
            "Ich rufe ein Tool auf.",
            [new Providers.ToolCall("call-1", "my_tool", "{\"path\":\"\"}")]));
        provider.Enqueue(new LlmChatResponse("Fehler verarbeitet.", []));

        var fakeTool = new FakeTool("my_tool", throwException: new ArgumentException("The path is empty. (Parameter 'path')"));
        var registry = new ToolRegistry([fakeTool]);
        var sut      = new ServerChatRunner(new ConfigurationService(), provider, registry);

        var tools = new[] { new Providers.ToolDefinition("my_tool", "Ein Tool", new { }) };
        var opts  = new ServerChatOptions(
            Prompt:   "Benutze Tool",
            History:  [],
            Model:    null,
            Verbose:  false,
            Tools:    tools);

        var result = await sut.RunServerChatAsync(opts);

        Assert.Equal("Fehler verarbeitet.", result.Response);
        Assert.Equal(2, provider.CallCount);
        Assert.Equal(1, fakeTool.CallCount);
        Assert.NotNull(provider.LastRequestMessages);
        Assert.Contains(provider.LastRequestMessages!, m =>
            m.Role == ChatRole.Tool &&
            m.ToolCallId == "call-1" &&
            m.Content.Contains("The path is empty.", StringComparison.Ordinal));
    }

    // â”€â”€ LlmExchanges-Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

}

// â”€â”€ FakeTool â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

internal sealed class FakeTool : BashGPT.Tools.Abstractions.ITool
{
    private readonly string _returnValue;
    private readonly Exception? _throwException;

    public int CallCount { get; private set; }

    public BashGPT.Tools.Abstractions.ToolDefinition Definition { get; }

    public FakeTool(string name, string returnValue = "Tool-Ergebnis", Exception? throwException = null)
    {
        _returnValue = returnValue;
        _throwException = throwException;
        Definition   = new BashGPT.Tools.Abstractions.ToolDefinition(name, "Fake-Tool fuer Tests", []);
    }

    public Task<BashGPT.Tools.Abstractions.ToolResult> ExecuteAsync(
        BashGPT.Tools.Abstractions.ToolCall call, CancellationToken ct)
    {
        CallCount++;
        if (_throwException is not null)
            throw _throwException;

        return Task.FromResult(new BashGPT.Tools.Abstractions.ToolResult(true, _returnValue));
    }
}
