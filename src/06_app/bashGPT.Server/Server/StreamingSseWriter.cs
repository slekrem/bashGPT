using System.Text.Json;
using bashGPT.Core.Serialization;
using bashGPT.Shell;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class StreamingSseWriter(Stream stream)
{
    public void WriteContentToken(string token)
    {
        var json = JsonSerializer.Serialize(
            new { choices = new[] { new { delta = new { content = token } } } },
            JsonDefaults.Options);
        ApiResponse.WriteSseEvent(stream, json);
    }

    public void WriteReasoningToken(string token)
    {
        var json = JsonSerializer.Serialize(
            new { choices = new[] { new { delta = new { reasoning = token } } } },
            JsonDefaults.Options);
        ApiResponse.WriteSseEvent(stream, json);
    }

    public void WriteEvent(SseEvent evt)
    {
        var json = JsonSerializer.Serialize(
            new { choices = new[] { new { delta = new { content = "", bashgpt = new { @event = evt.Event, data = evt.Data } } } } },
            JsonDefaults.Options);
        ApiResponse.WriteSseEvent(stream, json);
    }

    public void WriteDone(ServerChatResult result, string requestId)
    {
        var doneJson = JsonSerializer.Serialize(new
        {
            choices = new[] { new { delta = new { content = "" } } },
            usage = result.Usage == null ? null : (object)new
            {
                promptTokens = result.Usage.InputTokens,
                completionTokens = result.Usage.OutputTokens,
            },
            bashgpt = new
            {
                @event = "done",
                response = result.Response,
                usedToolCalls = result.UsedToolCalls,
                finalStatus = result.FinalStatus,
                requestId,
                logs = result.Logs,
                commands = result.Commands,
            },
        }, JsonDefaults.Options);

        ApiResponse.WriteSseEvent(stream, doneJson);
        WriteDoneMarker();
    }

    public void WriteCancelled(string requestId)
    {
        var cancelledJson = JsonSerializer.Serialize(new
        {
            choices = new[] { new { delta = new { content = "" } } },
            bashgpt = new
            {
                @event = "done",
                response = "Cancelled by user.",
                usedToolCalls = false,
                finalStatus = "user_cancelled",
                requestId,
                logs = Array.Empty<string>(),
                commands = Array.Empty<object>(),
            },
        }, JsonDefaults.Options);

        ApiResponse.WriteSseEvent(stream, cancelledJson);
        WriteDoneMarker();
    }

    public void WriteError(string message)
    {
        var errJson = JsonSerializer.Serialize(
            new { choices = new[] { new { delta = new { content = "", bashgpt = new { @event = "error", data = new { message } } } } } },
            JsonDefaults.Options);
        ApiResponse.WriteSseEvent(stream, errJson);
        WriteDoneMarker();
    }

    private void WriteDoneMarker() => ApiResponse.WriteSseEvent(stream, "[DONE]");
}
