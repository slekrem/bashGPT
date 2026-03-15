using BashGPT.Shell;

namespace BashGPT.Configuration;

public enum ProviderType
{
    Ollama,
    Cerebras
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
    public RateLimitingConfig RateLimiting { get; set; } = new();
}
