using System.Text.Json;
using bashGPT.Core.Serialization;

namespace bashGPT.Server;

internal sealed class ChatCancelApiHandler(RunningChatRegistry runningChats)
{
    public async Task HandleAsync(HttpContext ctx, CancellationToken ct)
    {
        var body = await ctx.Request.ReadFromJsonAsync<CancelRequest>(JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.RequestId))
        {
            await ctx.Response.WriteJsonAsync(new { error = "requestId is required." }, statusCode: 400);
            return;
        }

        var cancelled = runningChats.Cancel(body.RequestId.Trim());
        await ctx.Response.WriteJsonAsync(new
        {
            ok = true,
            cancelled,
            requestId = body.RequestId.Trim(),
        });
    }

    private sealed record CancelRequest(string RequestId);
}
