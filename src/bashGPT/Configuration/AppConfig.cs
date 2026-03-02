namespace BashGPT.Configuration;

public enum ProviderType
{
    Ollama,
    Cerebras
}

public class OllamaConfig
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gpt-oss:20b";
    public double? Temperature { get; set; } = 0.2;
    public double? TopP { get; set; } = 0.9;
    public int? NumCtx { get; set; } = 16384;
    public int? NumPredict { get; set; } = 1024;
    public double? RepeatPenalty { get; set; } = 1.05;
    public int? Seed { get; set; }
}

public class CerebrasConfig
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-oss:120b-cloud";
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1";
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxCompletionTokens { get; set; }
    public int? Seed { get; set; }
    public string? ReasoningEffort { get; set; }
}

public class AppConfig
{
    public ProviderType DefaultProvider { get; set; } = ProviderType.Ollama;
    public OllamaConfig Ollama { get; set; } = new();
    public CerebrasConfig Cerebras { get; set; } = new();
}
