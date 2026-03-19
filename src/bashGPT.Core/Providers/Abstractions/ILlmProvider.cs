using bashGPT.Core.Models.Providers;

namespace bashGPT.Core.Providers;

public interface ILlmProvider
{
    string Name { get; }
    string Model { get; }

    Task<LlmChatResponse> ChatAsync(
        LlmChatRequest request,
        CancellationToken ct = default);

    Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);

    IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);
}
