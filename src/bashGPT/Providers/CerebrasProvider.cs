using BashGPT.Configuration;

namespace BashGPT.Providers;

public class CerebrasProvider(CerebrasConfig config) : ILlmProvider
{
    public string Name  => "Cerebras";
    public string Model => config.Model;

    public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        => throw new NotImplementedException("CerebrasProvider wird in Issue #5 implementiert.");

    public async IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
#pragma warning disable CS0162
        await Task.CompletedTask;
        throw new NotImplementedException("CerebrasProvider wird in Issue #5 implementiert.");
        yield break;
#pragma warning restore CS0162
    }
}
