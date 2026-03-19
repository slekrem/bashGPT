using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleToolChoiceFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}
