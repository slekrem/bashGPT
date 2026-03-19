using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleChatResponse
{
    [JsonPropertyName("choices")] public List<OpenAiCompatibleChatChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiCompatibleUsage? Usage { get; set; }
}
