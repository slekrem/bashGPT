using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleDelta
{
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("reasoning")] public string? ReasoningContent { get; set; }
    [JsonPropertyName("tool_calls")] public List<OpenAiCompatibleToolCallDelta>? ToolCalls { get; set; }
}
