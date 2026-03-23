namespace bashGPT.Server;

internal sealed class ChatCancelApiHandler(RunningChatRegistry runningChats)
{
    public async Task<IResult> PostAsync(HttpRequest req, CancellationToken ct)
    {
        var body = await req.ReadFromJsonAsync<CancelRequest>(ct);
        if (body is null || string.IsNullOrWhiteSpace(body.RequestId))
            return Results.Json(new { error = "requestId is required." }, statusCode: 400);

        var cancelled = runningChats.Cancel(body.RequestId.Trim());
        return Results.Json(new
        {
            ok = true,
            cancelled,
            requestId = body.RequestId.Trim(),
        });
    }

    private sealed record CancelRequest(string RequestId);
}
