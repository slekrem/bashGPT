using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Tests.Cli;

/// <summary>
/// Tests für SSE-Streaming-Callbacks in ServerChatRunner (OnToken / OnEvent).
/// </summary>
public sealed class ServerChatRunnerStreamingTests
{
    private static ServerChatRunner CreateRunner(FakeLlmProvider provider) =>
        new(new ConfigurationService(), new ShellContextCollector(), provider);

    private static ServerChatOptions Opts(
        string prompt = "Hallo",
        ExecutionMode execMode = ExecutionMode.DryRun,
        Action<string>? onToken = null,
        Action<SseEvent>? onEvent = null) =>
        new(
            Prompt:     prompt,
            History:    [],
            Provider:   null,
            Model:      null,
            NoContext:  true,
            IncludeDir: false,
            ExecMode:   execMode,
            Verbose:    false,
            ForceTools: false,
            OnToken:    onToken,
            OnEvent:    onEvent);

    private static ToolCall BashCall(string cmd, string id = "tc-1") =>
        new(id, "bash", $$"""{"command":"{{cmd}}"}""");

    [Fact]
    public async Task OnToken_IsCalled_ForEachProviderToken()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("Hallo!", []));

        var tokens = new List<string>();
        var opts = Opts(onToken: t => tokens.Add(t));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(opts);

        Assert.NotEmpty(tokens);
        Assert.Equal("Hallo!", string.Concat(tokens));
    }

    [Fact]
    public async Task OnEvent_ToolCall_IsFired_AfterParsingCommands()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls -la", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("Fertig.", []));

        var events = new List<SseEvent>();
        var opts = Opts(onEvent: e => events.Add(e));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(opts);

        var toolCallEvent = events.FirstOrDefault(e => e.Event == "tool_call");
        Assert.NotNull(toolCallEvent);
    }

    [Fact]
    public async Task OnEvent_CommandResult_IsFired_AfterExecution()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("pwd", "tc-1")]));
        provider.Enqueue(new LlmChatResponse("Erledigt.", []));

        var events = new List<SseEvent>();
        var opts = Opts(onEvent: e => events.Add(e));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(opts);

        Assert.Contains(events, e => e.Event == "command_result");
    }

    [Fact]
    public async Task OnEvent_RoundStart_IsFired_ForSubsequentRounds()
    {
        var provider = new FakeLlmProvider();
        provider.Enqueue(new LlmChatResponse("", [BashCall("ls",  "tc-1")]));
        provider.Enqueue(new LlmChatResponse("", [BashCall("pwd", "tc-2")]));
        provider.Enqueue(new LlmChatResponse("Fertig.", []));

        var events = new List<SseEvent>();
        var opts = Opts(onEvent: e => events.Add(e));
        var sut = CreateRunner(provider);

        await sut.RunServerChatAsync(opts);

        Assert.Contains(events, e => e.Event == "round_start");
    }
}
