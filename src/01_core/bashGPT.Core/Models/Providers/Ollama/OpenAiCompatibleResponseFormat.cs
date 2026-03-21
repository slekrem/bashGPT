using System.Text.Json.Serialization;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleResponseFormat
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";

    public static OpenAiCompatibleResponseFormat? FromString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new OpenAiCompatibleResponseFormat { Type = value };
}
