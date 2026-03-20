using bashGPT.Core.Chat;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace BashGPT.Core.Tests.Chat;

public sealed class ChatSessionBootstrapTests
{
    [Fact]
    public async Task CreateAsync_WithConfigBackedToolChoice_CreatesSession()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-core-tests-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, """
            {
              "defaultForceTools": true,
              "ollama": {
                "baseUrl": "http://localhost:11434",
                "model": "test-model"
              }
            }
            """);

            var configService = new TestConfigurationService(configPath);
            var result = await ChatSessionBootstrap.CreateAsync(
                configService,
                modelOverride: null,
                tools: [new ToolDefinition("bash", "Shell", new { })],
                history: [],
                prompt: "Hello",
                toolChoiceFactory: config => config.DefaultForceTools ? "bash" : null);

            Assert.Null(result.Error);
            Assert.NotNull(result.Config);
            Assert.NotNull(result.Provider);
            Assert.NotNull(result.Session);
            Assert.Equal("bash", result.Session!.ToolChoiceName);
            Assert.Equal(ChatRole.User, result.Session.Messages[^1].Role);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task CreateAsync_WithProviderOverride_CreatesSessionWithoutConfig()
    {
        var configService = new ThrowingConfigurationService();
        var provider = new FakeProvider();

        var result = await ChatSessionBootstrap.CreateAsync(
            configService,
            modelOverride: null,
            tools: [],
            history: [],
            prompt: "Hello",
            providerOverride: provider);

        Assert.Null(result.Error);
        Assert.Null(result.Config);
        Assert.Same(provider, result.Provider);
        Assert.NotNull(result.Session);
        Assert.Equal(ChatRole.User, result.Session!.Messages[^1].Role);
    }

    private sealed class TestConfigurationService(string configFile) : ConfigurationService
    {
        protected override string ConfigFile => configFile;
    }

    private sealed class ThrowingConfigurationService : ConfigurationService
    {
        protected override string ConfigFile => throw new InvalidOperationException("Should not be used.");
    }

    private sealed class FakeProvider : ILlmProvider
    {
        public string Name => "fake";
        public string Model => "fake";

        public Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
