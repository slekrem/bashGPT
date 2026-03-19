using System.Net;
using System.Text.Json;
using bashGPT.Core.Serialization;

namespace BashGPT.Server;

internal sealed class ChatCancelApiHandler(RunningChatRegistry runningChats)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<CancelRequest>(ctx.Request.InputStream, JsonDefaults.Options, ct);
        if (body is null || string.IsNullOrWhiteSpace(body.RequestId))
        {
            await ApiResponse.WriteJsonAsync(ctx.Response, new { error = "requestId fehlt." }, statusCode: 400);
            return;
        }

        var cancelled = runningChats.Cancel(body.RequestId.Trim());
        await ApiResponse.WriteJsonAsync(ctx.Response, new
        {
            ok = true,
            cancelled,
            requestId = body.RequestId.Trim(),
        });
    }

    private sealed record CancelRequest(string RequestId);
}
