namespace bashGPT.Core.Configuration;

public class AppConfig
{
    public bool DefaultForceTools { get; set; } = false;
    public OllamaConfig Ollama { get; set; } = new();
}
