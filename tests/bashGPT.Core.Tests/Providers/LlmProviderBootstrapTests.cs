using bashGPT.Core.Configuration;
using bashGPT.Core.Providers;
using bashGPT.Core.Providers.Abstractions;

namespace BashGPT.Core.Tests.Providers;

public sealed class LlmProviderBootstrapTests
{
    [Fact]
    public async Task CreateAsync_WithProviderOverride_ReturnsOverrideWithoutLoadingConfig()
    {
        var configService = new ThrowingConfigurationService();
        var provider = new FakeProvider();

        var result = await LlmProviderBootstrap.CreateAsync(configService, "ignored", provider);

        Assert.Same(provider, result.Provider);
        Assert.Null(result.Config);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithModelOverride_AppliesOverrideToProvider()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-core-tests-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, """
            {
              "ollama": {
                "baseUrl": "http://localhost:11434",
                "model": "original-model"
              }
            }
            """);

            var configService = new TestConfigurationService(configPath);
            var result = await LlmProviderBootstrap.CreateAsync(configService, "override-model");

            Assert.NotNull(result.Config);
            Assert.NotNull(result.Provider);
            Assert.Null(result.Error);
            Assert.Equal("override-model", result.Config!.Ollama.Model);
            Assert.Equal("override-model", result.Provider!.Model);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task CreateAsync_WhenConfigLoadFails_ReturnsConfigurationError()
    {
        var configPath = Path.Combine(Path.GetTempPath(), $"bashgpt-core-tests-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(configPath, "{ invalid-json ");
            var configService = new TestConfigurationService(configPath);

            var result = await LlmProviderBootstrap.CreateAsync(configService, null);

            Assert.Null(result.Config);
            Assert.Null(result.Provider);
            Assert.StartsWith("Configuration error:", result.Error, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
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

        public Task<bashGPT.Core.Models.Providers.LlmChatResponse> ChatAsync(
            bashGPT.Core.Models.Providers.LlmChatRequest request,
            CancellationToken ct = default) => throw new NotSupportedException();

        public Task<string> CompleteAsync(
            IEnumerable<bashGPT.Core.Models.Providers.ChatMessage> messages,
            CancellationToken ct = default) => throw new NotSupportedException();

        public IAsyncEnumerable<string> StreamAsync(
            IEnumerable<bashGPT.Core.Models.Providers.ChatMessage> messages,
            CancellationToken ct = default) => throw new NotSupportedException();
    }
}
