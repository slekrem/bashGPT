using bashGPT.Core.Chat;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace BashGPT.Core.Tests.Chat;

public sealed class ChatSessionStateTests
{
    [Fact]
    public void InitializeMessages_WithSystemPrompt_AddsSystemHistoryAndPrompt()
    {
        var provider = new FakeSessionProvider();
        var session = new ChatSessionState(
            provider,
            [],
            systemPrompt: () => ["System prompt"]);

        session.InitializeMessages([new ChatMessage(ChatRole.Assistant, "Earlier")], "Hello");

        Assert.Equal(3, session.Messages.Count);
        Assert.Equal(ChatRole.System, session.Messages[0].Role);
        Assert.Equal(ChatRole.Assistant, session.Messages[1].Role);
        Assert.Equal(ChatRole.User, session.Messages[2].Role);
    }

    [Fact]
    public async Task CallOnceAsync_TracksUsageAndExchange()
    {
        var provider = new FakeSessionProvider();
        provider.Enqueue(new LlmChatResponse("Answer", [], new TokenUsage(7, 3)));
        var session = new ChatSessionState(provider, []);
        session.InitializeMessages([], "Hello");

        var result = await session.CallOnceAsync(null, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal("Answer", result.Response.Content);
        Assert.NotNull(session.BuildUsage());
        Assert.Equal(7, session.BuildUsage()!.InputTokens);
        Assert.Equal(3, session.BuildUsage()!.OutputTokens);
        Assert.Single(session.LlmExchanges);
    }

    [Fact]
    public void RefreshSystemMessages_ReplacesLeadingSystemMessagesOnly()
    {
        var provider = new FakeSessionProvider();
        var session = new ChatSessionState(
            provider,
            [],
            systemPrompt: () => ["New system"]);

        session.Messages.Add(new ChatMessage(ChatRole.System, "Old system"));
        session.Messages.Add(new ChatMessage(ChatRole.User, "Hello"));

        session.RefreshSystemMessages();

        Assert.Equal(2, session.Messages.Count);
        Assert.Equal("New system", session.Messages[0].Content);
        Assert.Equal("Hello", session.Messages[1].Content);
    }

    [Fact]
    public void ChatSessionFactory_Create_InitializesSessionWithHistoryAndPrompt()
    {
        var provider = new FakeSessionProvider();

        var session = ChatSessionFactory.Create(
            provider,
            [],
            [new ChatMessage(ChatRole.Assistant, "Earlier")],
            "Prompt",
            systemPrompt: () => ["System"]);

        Assert.Equal(3, session.Messages.Count);
        Assert.Equal(ChatRole.System, session.Messages[0].Role);
        Assert.Equal(ChatRole.Assistant, session.Messages[1].Role);
        Assert.Equal(ChatRole.User, session.Messages[2].Role);
    }

    [Fact]
    public async Task CreateCompletedOutcome_UsesLastResponseAndUsage()
    {
        var provider = new FakeSessionProvider();
        provider.Enqueue(new LlmChatResponse("Done", [], new TokenUsage(4, 2)));
        var session = new ChatSessionState(provider, []);
        session.InitializeMessages([], "Hello");

        await session.CallOnceAsync(null, CancellationToken.None);
        var outcome = session.CreateCompletedOutcome("timeout");

        Assert.Equal("Done", outcome.Response);
        Assert.Equal("timeout", outcome.FinalStatus);
        Assert.NotNull(outcome.Usage);
        Assert.Single(outcome.LlmExchanges!);
    }

    [Fact]
    public void CreateCancelledOutcome_UsesProvidedStatusAndCurrentUsage()
    {
        var provider = new FakeSessionProvider();
        var session = new ChatSessionState(provider, []);

        var outcome = session.CreateCancelledOutcome();

        Assert.Equal("Cancelled by user.", outcome.Response);
        Assert.Equal("user_cancelled", outcome.FinalStatus);
        Assert.Null(outcome.Usage);
        Assert.Null(outcome.LlmExchanges);
    }

    [Fact]
    public async Task RunToolCallLoopAsync_RunsRoundsAndCallsBeforeNextCall()
    {
        var provider = new FakeSessionProvider();
        provider.Enqueue(new LlmChatResponse("After tool", []));
        var session = new ChatSessionState(provider, []);
        session.InitializeMessages([], "Hello");

        var initial = new LlmChatResponse("Tool round", [new ToolCall("call-1", "bash", """{"command":"pwd"}""")]);
        var rounds = new List<int>();
        var beforeNextCallCount = 0;

        var result = await session.RunToolCallLoopAsync(
            initial,
            (round, response) =>
            {
                rounds.Add(round);
                Assert.Single(response.ToolCalls);
                return Task.CompletedTask;
            },
            () => beforeNextCallCount++,
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(1, result.Rounds);
        Assert.Equal("After tool", result.Response.Content);
        Assert.Equal([1], rounds);
        Assert.Equal(1, beforeNextCallCount);
    }

    private sealed class FakeSessionProvider : ILlmProvider
    {
        private readonly Queue<LlmChatResponse> _responses = new();

        public string Name => "Fake";
        public string Model => "fake-model";

        public void Enqueue(LlmChatResponse response) => _responses.Enqueue(response);

        public Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
            => Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new LlmChatResponse("", []));

        public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public async IAsyncEnumerable<string> StreamAsync(
            IEnumerable<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
