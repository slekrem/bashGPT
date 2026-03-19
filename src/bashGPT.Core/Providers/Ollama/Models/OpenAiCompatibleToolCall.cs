using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiCompatibleToolCallFunction Function { get; set; } = new();
}
