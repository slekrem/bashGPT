using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers.Ollama;
using bashGPT.Core.Configuration;

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
    public void OllamaProvider_UsesConfiguredDefaults()
    {
        var config = new AppConfig();
        var provider = new OllamaProvider(config.Ollama);

        Assert.Equal("Ollama", provider.Name);
        Assert.Equal(config.Ollama.Model, provider.Model);
    }
}
