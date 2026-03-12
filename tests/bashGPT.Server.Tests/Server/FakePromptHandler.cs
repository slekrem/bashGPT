using BashGPT.Cli;

namespace BashGPT.Server.Tests;

/// <summary>
/// Test-Stub für IPromptHandler – gibt konfigurierbare Dummy-Antworten zurück
/// ohne echte LLM-Aufrufe zu machen.
/// </summary>
internal sealed class FakePromptHandler : IPromptHandler
{
    public ServerChatResult NextResult { get; set; } = new(
        Response: "Fake-Antwort vom LLM.",
        Logs: []);

    public Exception? NextException { get; set; }
    public bool WaitForCancellation { get; set; }

    public ServerChatOptions? LastOptions { get; private set; }
    public int CallCount { get; private set; }

    public async Task<ServerChatResult> RunServerChatAsync(ServerChatOptions opts, CancellationToken ct = default)
    {
        LastOptions = opts;
        CallCount++;

        if (WaitForCancellation)
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);

        if (NextException is not null)
            throw NextException;

        return NextResult;
    }
}
