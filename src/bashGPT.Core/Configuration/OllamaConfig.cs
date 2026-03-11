namespace BashGPT.Configuration;

public class OllamaConfig
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gpt-oss:20b";
    public double? Temperature { get; set; } = 0.2;
    public double? TopP { get; set; } = 0.9;
    public int? Seed { get; set; }
    public int? NumCtx { get; set; } = 65536;
}
