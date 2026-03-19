using System.Text.Json.Serialization;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleChatChoice
{
    [JsonPropertyName("message")] public OpenAiCompatibleMessage? Message { get; set; }
}
