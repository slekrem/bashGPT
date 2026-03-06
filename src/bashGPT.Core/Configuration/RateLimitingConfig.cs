namespace BashGPT.Configuration;

public class RateLimitingConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 30;
    public int AgentRequestDelayMs { get; set; } = 500;
}
