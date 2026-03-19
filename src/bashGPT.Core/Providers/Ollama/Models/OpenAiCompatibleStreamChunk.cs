using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleStreamChunk
{
    [JsonPropertyName("choices")] public List<OpenAiCompatibleChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiCompatibleUsage? Usage { get; set; }
}
