using System.Text;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleToolCallBuilder
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public StringBuilder Arguments { get; } = new();
    public int Index { get; init; }

    public ToolCall ToToolCall() =>
        new(Id, Name ?? "", Arguments.ToString(), Index);
}
