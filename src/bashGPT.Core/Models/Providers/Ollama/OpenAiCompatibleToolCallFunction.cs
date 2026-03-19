using System.Text.Json.Serialization;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleToolCallFunction
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("arguments")] public string? Arguments { get; set; }
}
