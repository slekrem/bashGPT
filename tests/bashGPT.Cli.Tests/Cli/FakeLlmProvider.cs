using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using System.Runtime.CompilerServices;

namespace BashGPT.Cli.Tests;

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
    public Func<int, Exception?>? ExceptionForCall { get; set; }
    public IReadOnlyList<ChatMessage>? LastRequestMessages { get; private set; }

    public void Enqueue(LlmChatResponse response) => _queue.Enqueue(response);

    public async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        LastRequestMessages = request.Messages.ToList();
        CallCount++;

        if (ExceptionForCall?.Invoke(CallCount) is { } exForCall)
            throw exForCall;

        if (NextException is not null)
            throw NextException;

        var response = _queue.Count > 0 ? _queue.Dequeue() : new LlmChatResponse("", []);

        // Simulate OnRequestJson/OnResponseJson callbacks with synthetic JSON
        if (request.OnRequestJson is not null)
            await request.OnRequestJson($"{{\"call\":{CallCount},\"type\":\"request\"}}");
        if (request.OnResponseJson is not null)
            await request.OnResponseJson($"{{\"call\":{CallCount},\"type\":\"response\"}}");

        // Simulate token streaming: invoke OnToken per character of Content
        if (request.OnToken is not null && !string.IsNullOrEmpty(response.Content))
        {
            foreach (var ch in response.Content)
                request.OnToken(ch.ToString());
        }

        return response;
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
