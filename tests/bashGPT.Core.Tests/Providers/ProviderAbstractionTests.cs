using BashGPT.Configuration;
using BashGPT.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace BashGPT.Core.Tests.Providers;

public class ProviderAbstractionTests
{
    [Fact]
    public void ChatMessage_RoleString_ReturnsCorrectValues()
    {
        Assert.Equal("system",    new ChatMessage(ChatRole.System,    "").RoleString);
        Assert.Equal("user",      new ChatMessage(ChatRole.User,      "").RoleString);
        Assert.Equal("assistant", new ChatMessage(ChatRole.Assistant, "").RoleString);
    }

    [Fact]
    public void ChatMessage_IsRecord_EqualsByValue()
    {
        var a = new ChatMessage(ChatRole.User, "Hallo");
        var b = new ChatMessage(ChatRole.User, "Hallo");
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(ProviderType.Ollama)]
    [InlineData(ProviderType.Cerebras)]
    public void ProviderFactory_Create_ReturnsCorrectProvider(ProviderType type)
    {
        var config = new AppConfig { DefaultProvider = type };
        var provider = ProviderFactory.Create(config);

        Assert.Equal(type == ProviderType.Ollama ? "Ollama" : "Cerebras", provider.Name);
    }

    [Fact]
    public void ProviderFactory_Create_OverrideWinsOverConfig()
    {
        var config = new AppConfig { DefaultProvider = ProviderType.Ollama };
        var provider = ProviderFactory.Create(config, overrideType: ProviderType.Cerebras);

        Assert.Equal("Cerebras", provider.Name);
    }

    [Fact]
    public void ProviderFactory_Create_WhenRateLimitingEnabled_WrapsProvider()
    {
        var config = new AppConfig
        {
            DefaultProvider = ProviderType.Ollama,
            RateLimiting = new RateLimitingConfig { Enabled = true, MaxRequestsPerMinute = 30, AgentRequestDelayMs = 500 }
        };

        var provider = ProviderFactory.Create(config);

        Assert.IsType<RateLimitedLlmProvider>(provider);
    }

    [Fact]
    public void ProviderFactory_Create_WhenRateLimitingDisabled_ReturnsPlainProvider()
    {
        var config = new AppConfig
        {
            DefaultProvider = ProviderType.Ollama,
            RateLimiting = new RateLimitingConfig { Enabled = false, MaxRequestsPerMinute = 30, AgentRequestDelayMs = 500 }
        };

        var provider = ProviderFactory.Create(config);

        Assert.IsType<OllamaProvider>(provider);
    }

    [Fact]
    public void AddBashGptProviders_RegistersSingletonLlmProvider()
    {
        var services = new ServiceCollection();
        var config = new AppConfig
        {
            DefaultProvider = ProviderType.Ollama,
            RateLimiting = new RateLimitingConfig { Enabled = false }
        };

        services.AddBashGptProviders(config);
        using var scope = services.BuildServiceProvider().CreateScope();
        var provider1 = scope.ServiceProvider.GetRequiredService<ILlmProvider>();
        var provider2 = scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        Assert.Same(provider1, provider2);
    }

    // Name/Model und Implementierungsdetails der Provider werden
    // in OllamaProviderTests und CerebrasProviderTests getestet.
}
