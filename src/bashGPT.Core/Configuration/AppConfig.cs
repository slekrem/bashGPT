using BashGPT.Shell;

namespace BashGPT.Configuration;

public enum ProviderType
{
    Ollama
}

public class AppConfig
{
    public ProviderType DefaultProvider { get; set; } = ProviderType.Ollama;
    public ExecutionMode DefaultExecMode { get; set; } = ExecutionMode.Ask;
    public bool DefaultForceTools { get; set; } = false;
    public OllamaConfig Ollama { get; set; } = new();
}
