namespace BashGPT.Configuration;

public class CerebrasConfig
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-oss:120b-cloud";
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1";
}
