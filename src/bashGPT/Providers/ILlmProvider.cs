namespace BashGPT.Providers;

public interface ILlmProvider
{
    string Name { get; }
    string Model { get; }

    Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);

    IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);
}
