namespace BashGPT.Configuration;

public class CerebrasConfig
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-oss:120b-cloud";
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1";
    public double? Temperature { get; set; } = 0.2;
    public double? TopP { get; set; } = 0.9;
    public int? MaxCompletionTokens { get; set; } = 2048;
    public int? Seed { get; set; }
    public string? ReasoningEffort { get; set; } = "medium";
}
