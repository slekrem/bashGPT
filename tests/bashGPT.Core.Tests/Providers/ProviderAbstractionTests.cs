using BashGPT.Configuration;
using BashGPT.Providers;

namespace BashGPT.Tests.Providers;

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

    // Name/Model und Implementierungsdetails der Provider werden
    // in OllamaProviderTests und CerebrasProviderTests getestet.
}
