using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleToolChoice
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiCompatibleToolChoiceFunction Function { get; set; } = new();

    public static OpenAiCompatibleToolChoice ForFunction(string name) =>
        new() { Function = new OpenAiCompatibleToolChoiceFunction { Name = name } };
}
