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
}

public class CerebrasConfig
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-oss:120b-cloud";
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1";
}

public class AppConfig
{
    public ProviderType DefaultProvider { get; set; } = ProviderType.Ollama;
    public OllamaConfig Ollama { get; set; } = new();
    public CerebrasConfig Cerebras { get; set; } = new();
}
