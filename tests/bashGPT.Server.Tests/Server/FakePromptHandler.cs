namespace bashGPT.Server.Tests;

/// <summary>
/// Test stub for <see cref="IChatHandler"/> that returns configurable dummy responses
/// without making real LLM calls.
/// </summary>
internal sealed class FakePromptHandler : IChatHandler
{
    public ServerChatResult NextResult { get; set; } = new(
        Response: "Fake response from the LLM.",
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
