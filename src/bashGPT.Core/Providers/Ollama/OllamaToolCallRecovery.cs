using System.Text.Json;

namespace BashGPT.Providers;

internal static class OllamaToolCallRecovery
{
    public static LlmChatResponse? TryRecover(string errorBody, string? toolName)
    {
        if (toolName is null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            var message = doc.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString();

            if (message is null || !message.Contains("error parsing tool call", StringComparison.Ordinal))
                return null;

            var rawStart = message.IndexOf("raw='", StringComparison.Ordinal);
            if (rawStart < 0)
                return null;

            rawStart += "raw='".Length;

            var rawEnd = message.LastIndexOf("', err=", StringComparison.Ordinal);
            if (rawEnd < 0 || rawEnd <= rawStart)
                return null;

            var raw = message[rawStart..rawEnd];
            var jsonStart = raw.LastIndexOf('{');
            if (jsonStart < 0)
                return null;

            var jsonString = raw[jsonStart..];
            JsonDocument.Parse(jsonString).Dispose();

            var reasoningText = raw[..jsonStart].Trim();
            var toolCall = new ToolCall("recovered-0", toolName, jsonString);
            return new LlmChatResponse(reasoningText, [toolCall], null);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
