using BashGPT.Providers;
using System.Runtime.CompilerServices;

namespace BashGPT.Tests.Cli;

/// <summary>
/// Test-Stub für ILlmProvider – gibt konfigurierbare LlmChatResponse-Objekte zurück,
/// ohne echte LLM-Aufrufe zu machen.
/// </summary>
internal sealed class FakeLlmProvider : ILlmProvider
{
    private readonly Queue<LlmChatResponse> _queue = new();

    public string Name  => "fake";
    public string Model => "fake-model";

    public int CallCount { get; private set; }
    public Exception? NextException { get; set; }
    public IReadOnlyList<ChatMessage>? LastRequestMessages { get; private set; }

    public void Enqueue(LlmChatResponse response) => _queue.Enqueue(response);

    public Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        LastRequestMessages = request.Messages.ToList();
        CallCount++;

        if (NextException is not null)
            throw NextException;

        var response = _queue.Count > 0 ? _queue.Dequeue() : new LlmChatResponse("", []);
        return Task.FromResult(response);
    }

    public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        => Task.FromResult("fake");

    public async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "fake";
        await Task.CompletedTask;
    }
}
