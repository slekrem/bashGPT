using bashGPT.Core.Models.Providers;

namespace bashGPT.Core.Chat;

public static class ChatSessionRunner
{
    public static async Task<ChatSessionRunResult> RunAsync(
        ChatSessionState session,
        Action<string>? onToken,
        bool enableToolCalls,
        Func<int, LlmChatResponse, Task>? executeRoundAsync,
        Action? beforeNextCall,
        CancellationToken ct)
    {
        var first = await session.CallOnceAsync(onToken, ct);
        if (first.Error is not null)
            return new ChatSessionRunResult(first.Response, first.Error, UsedToolCalls: false, ToolCallRounds: 0);

        if (!enableToolCalls || first.Response.ToolCalls.Count == 0)
            return new ChatSessionRunResult(first.Response, Error: null, UsedToolCalls: false, ToolCallRounds: 0);

        ArgumentNullException.ThrowIfNull(executeRoundAsync);

        var loop = await session.RunToolCallLoopAsync(
            first.Response,
            executeRoundAsync,
            beforeNextCall,
            ct);

        return new ChatSessionRunResult(loop.Response, loop.Error, UsedToolCalls: true, ToolCallRounds: loop.Rounds);
    }
}
