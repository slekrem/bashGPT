using BashGPT.Agents;
using BashGPT.Providers;
using BashGPT.Storage;

namespace BashGPT.Agents.Shell.Tests;

public class ShellAgentCheckTests : IDisposable
{
    private readonly string _tempFile;
    private readonly SessionStore _sessionStore;

    public ShellAgentCheckTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"shell-agent-test-{Guid.NewGuid():N}.json");
        _sessionStore = new SessionStore(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (File.Exists(_tempFile + ".tmp")) File.Delete(_tempFile + ".tmp");
    }

    // ── Fehlerfälle ────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_NullProvider_Returns_NoProviderResult()
    {
        var check = new ShellAgentCheck(provider: null);
        var agent = MakeAgent("ag-001");

        var result = await check.RunAsync(agent, CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(result.Changed);
        Assert.Contains("Kein LLM-Provider", result.Message);
    }

    [Fact]
    public async Task RunAsync_EmptyLoopInstruction_UsesDefaultInstruction()
    {
        var provider = new FakeProvider([TextResponse("Hallo")]);
        var check = new ShellAgentCheck(provider);
        var agent = MakeAgent("ag-002", loopInstruction: "");

        var result = await check.RunAsync(agent, CancellationToken.None);

        // Leere LoopInstruction ist erlaubt – Default-Instruction wird verwendet
        Assert.True(result.Success);
        Assert.Equal("Hallo", result.Message);
    }

    // ── Erfolgreiche Ausführung ────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_PlainTextResponse_Returns_SuccessWithHash()
    {
        var provider = new FakeProvider([TextResponse("Alles OK.")]);
        var check = new ShellAgentCheck(provider);
        var agent = MakeAgent("ag-003");

        var result = await check.RunAsync(agent, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Alles OK.", result.Message);
        Assert.NotEmpty(result.Hash);
        Assert.False(result.Changed);
    }

    [Fact]
    public async Task RunAsync_MakesExactlyOneLlmCall()
    {
        var provider = new FakeProvider([TextResponse("Einmalige Antwort.")]);
        var check = new ShellAgentCheck(provider);
        var agent = MakeAgent("ag-004");

        await check.RunAsync(agent, CancellationToken.None);

        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task RunAsync_SessionStore_PersistsMessages_AfterSuccess()
    {
        var provider = new FakeProvider([TextResponse("Ergebnis gespeichert.")]);
        var check = new ShellAgentCheck(provider, sessionStore: _sessionStore);
        var agent = MakeAgent("ag-005");

        await check.RunAsync(agent, CancellationToken.None);

        var session = await _sessionStore.LoadAsync($"agent-shell-{agent.Id}");
        Assert.NotNull(session);
        Assert.Equal(2, session.Messages.Count);
        Assert.Equal("user",      session.Messages[0].Role);
        Assert.Equal("assistant", session.Messages[1].Role);
        Assert.Equal("Ergebnis gespeichert.", session.Messages[1].Content);
    }

    [Fact]
    public async Task RunAsync_SessionStore_AppendsToPreviousSession()
    {
        var provider = new FakeProvider([TextResponse("Runde 1."), TextResponse("Runde 2.")]);
        var check = new ShellAgentCheck(provider, sessionStore: _sessionStore);
        var agent = MakeAgent("ag-006");

        await check.RunAsync(agent, CancellationToken.None);
        await check.RunAsync(agent, CancellationToken.None);

        var session = await _sessionStore.LoadAsync($"agent-shell-{agent.Id}");
        Assert.NotNull(session);
        Assert.Equal(4, session.Messages.Count); // 2x (user + assistant)
    }

    [Fact]
    public async Task RunAsync_CancelledToken_ThrowsOrReturnsEarly()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = new FakeProvider([TextResponse("Wird nie gesendet.")]);
        var check = new ShellAgentCheck(provider);
        var agent = MakeAgent("ag-007");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => check.RunAsync(agent, cts.Token));
    }

    [Fact]
    public async Task RunAsync_CustomSystemPrompt_IsUsed_NotContextCollector()
    {
        var provider = new FakeProvider([TextResponse("OK")]);
        var check = new ShellAgentCheck(provider);
        var agent = MakeAgent("ag-008", systemPrompt: "Mein eigener Prompt.");

        await check.RunAsync(agent, CancellationToken.None);

        var systemMsg = provider.LastRequest?.Messages[0];
        Assert.NotNull(systemMsg);
        Assert.Equal("Mein eigener Prompt.", systemMsg!.Content);
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private static AgentRecord MakeAgent(
        string id,
        string loopInstruction = "Überprüfe den Status.",
        string? systemPrompt = null) => new()
    {
        Id              = id,
        Name            = $"test-agent-{id}",
        Type            = AgentCheckType.Shell,
        IsActive        = true,
        LoopInstruction = loopInstruction,
        SystemPrompt    = systemPrompt,
    };

    private static LlmChatResponse TextResponse(string text)
        => new(text, []);

    // ── Fake Provider ──────────────────────────────────────────────────────

    private sealed class FakeProvider(LlmChatResponse[] responses) : ILlmProvider
    {
        private int _index;

        public string Name  => "fake";
        public string Model => "fake-model";
        public int CallCount => _index;
        public LlmChatRequest? LastRequest { get; private set; }

        public Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            LastRequest = request;
            var response = _index < responses.Length
                ? responses[_index]
                : new LlmChatResponse("Ende.", []);
            _index++;
            return Task.FromResult(response);
        }

        public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
            => Task.FromResult("fake");

        public async IAsyncEnumerable<string> StreamAsync(
            IEnumerable<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return "fake";
            await Task.CompletedTask;
        }
    }
}
