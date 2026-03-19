using bashGPT.Core.Providers;
using BashGPT.Cli;
using BashGPT.Configuration;
using BashGPT.Server;

namespace BashGPT.Cli.Tests;

/// <summary>
/// Tests für SSE-Streaming-Callbacks in ServerChatRunner (OnToken).
/// </summary>
public sealed class ServerChatRunnerStreamingTests
{
    private static ServerChatRunner CreateRunner(FakeLlmProvider provider) =>
        new(new ConfigurationService(), provider);

    private static ServerChatOptions Opts(
        string prompt = "Hallo",
        Action<string>? onToken = null) =>
        new(
            Prompt:   prompt,
            History:  [],
            Model:    null,
            Verbose:  false,
            OnToken:  onToken);

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
}
