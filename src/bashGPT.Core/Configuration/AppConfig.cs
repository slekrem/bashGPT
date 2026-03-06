using BashGPT.Shell;

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
    public int? Seed { get; set; }
}

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

public class RateLimitingConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 30;
    public int AgentRequestDelayMs { get; set; } = 500;
}

public class AppConfig
{
    public ProviderType DefaultProvider { get; set; } = ProviderType.Ollama;
    public int CommandTimeoutSeconds { get; set; } = AppDefaults.CommandTimeoutSeconds;
    public bool LoopDetectionEnabled { get; set; } = true;
    public int MaxToolCallRounds { get; set; } = AppDefaults.MaxToolCallRounds;
    public ExecutionMode DefaultExecMode { get; set; } = ExecutionMode.Ask;
    public bool DefaultForceTools { get; set; } = false;
    public OllamaConfig Ollama { get; set; } = new();
    public CerebrasConfig Cerebras { get; set; } = new();
    public RateLimitingConfig RateLimiting { get; set; } = new();
}
