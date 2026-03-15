using System.Text.Json;

namespace BashGPT.Providers;

public record ToolDefinition(string Name, string Description, object Parameters);

public static class ToolDefinitions
{
    public static readonly ToolDefinition Bash = new(
        Name: "bash",
        Description: "Führt einen Shell-Befehl aus",
        Parameters: new
        {
            type = "object",
            properties = new
            {
                command = new
                {
                    type = "string",
                    description = "Shell-Befehl"
                }
            },
            required = new[] { "command" }
        });
}

public record ToolCall(string? Id, string Name, string ArgumentsJson, int? Index = null);

public record LlmChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ToolDefinition>? Tools = null,
    string? ToolChoiceName = null,
    bool? ParallelToolCalls = null,
    bool Stream = true,
    Action<string>? OnToken = null,
    Action<string>? OnReasoningToken = null,
    Func<string, Task>? OnRequestJson = null,
    Func<string, Task>? OnResponseJson = null,
    double? Temperature = null,
    double? TopP = null,
    int? NumCtx = null,
    int? MaxTokens = null,
    int? Seed = null,
    string? ReasoningEffort = null,
    double? FrequencyPenalty = null,
    double? PresencePenalty = null,
    IReadOnlyList<string>? Stop = null,
    string? ResponseFormat = null);

public record TokenUsage(
    int InputTokens,
    int OutputTokens,
    int? TotalTokens = null,
    int? CachedInputTokens = null);

public record LlmChatResponse(
    string Content,
    IReadOnlyList<ToolCall> ToolCalls,
    TokenUsage? Usage = null);

public static class ToolCallParsing
{
    public static bool TryGetCommand(ToolCall call, out string command, out string? error)
    {
        command = "";
        error = null;

        if (!call.Name.Equals("bash", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unbekanntes Tool '{call.Name}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(call.ArgumentsJson))
        {
            error = "Leere Tool-Argumente.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(call.ArgumentsJson);
            if (!doc.RootElement.TryGetProperty("command", out var cmdEl))
            {
                error = "Tool-Argumente enthalten kein Feld 'command'.";
                return false;
            }

            if (cmdEl.ValueKind != JsonValueKind.String)
            {
                error = "Tool-Argument 'command' ist kein String.";
                return false;
            }

            command = cmdEl.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(command))
            {
                error = "Tool-Argument 'command' ist leer.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Ungültiges JSON in Tool-Argumenten: {ex.Message}";
            return false;
        }
    }
}
