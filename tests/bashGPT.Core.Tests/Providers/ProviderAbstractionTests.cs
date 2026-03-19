using bashGPT.Core.Providers;
using BashGPT.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BashGPT.Core.Tests.Providers;

public class ProviderAbstractionTests
{
    [Fact]
    public void ChatMessage_RoleString_ReturnsCorrectValues()
    {
        Assert.Equal("system", new ChatMessage(ChatRole.System, "").RoleString);
        Assert.Equal("user", new ChatMessage(ChatRole.User, "").RoleString);
        Assert.Equal("assistant", new ChatMessage(ChatRole.Assistant, "").RoleString);
    }

    [Fact]
    public void ChatMessage_IsRecord_EqualsByValue()
    {
        var a = new ChatMessage(ChatRole.User, "Hallo");
        var b = new ChatMessage(ChatRole.User, "Hallo");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ProviderFactory_Create_ReturnsOllamaProvider()
    {
        var config = new AppConfig();
        var provider = ProviderFactory.Create(config);

        Assert.Equal("Ollama", provider.Name);
    }

    [Fact]
    public void AddBashGptProviders_RegistersSingletonLlmProvider()
    {
        var services = new ServiceCollection();
        var config = new AppConfig();

        services.AddBashGptProviders(config);
        using var scope = services.BuildServiceProvider().CreateScope();
        var provider1 = scope.ServiceProvider.GetRequiredService<ILlmProvider>();
        var provider2 = scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        Assert.Same(provider1, provider2);
    }
}
