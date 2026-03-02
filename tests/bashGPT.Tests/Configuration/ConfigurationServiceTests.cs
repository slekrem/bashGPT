using BashGPT.Configuration;

namespace BashGPT.Tests.Configuration;

public class ConfigurationServiceTests : IDisposable
{
    // Jeder Test bekommt ein eigenes temporäres Config-Verzeichnis
    private readonly string _tmpDir;

    public ConfigurationServiceTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "bashgpt-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    private ConfigurationService CreateService() =>
        new TestableConfigurationService(_tmpDir);

    [Fact]
    public async Task Load_ReturnsDefaults_WhenNoFileExists()
    {
        var svc = CreateService();
        var config = await svc.LoadAsync();

        Assert.Equal(ProviderType.Ollama, config.DefaultProvider);
        Assert.Equal("http://localhost:11434", config.Ollama.BaseUrl);
        Assert.Equal("gpt-oss:20b", config.Ollama.Model);
        Assert.Equal(0.2, config.Ollama.Temperature);
        Assert.Equal(0.9, config.Ollama.TopP);
        Assert.Equal(16384, config.Ollama.NumCtx);
        Assert.Equal(1024, config.Ollama.NumPredict);
        Assert.Equal(1.05, config.Ollama.RepeatPenalty);
        Assert.Null(config.Ollama.Seed);
        Assert.Equal("gpt-oss:120b-cloud", config.Cerebras.Model);
        Assert.Equal(0.2, config.Cerebras.Temperature);
        Assert.Equal(0.9, config.Cerebras.TopP);
        Assert.Equal(2048, config.Cerebras.MaxCompletionTokens);
        Assert.Null(config.Cerebras.Seed);
        Assert.Equal("medium", config.Cerebras.ReasoningEffort);
        Assert.Null(config.Cerebras.ApiKey);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var svc = CreateService();
        var config = new AppConfig
        {
            DefaultProvider = ProviderType.Cerebras,
            Ollama = new OllamaConfig { Model = "gpt-oss:20b", BaseUrl = "http://localhost:11434" },
            Cerebras = new CerebrasConfig { ApiKey = "test-key", Model = "gpt-oss:120b-cloud" }
        };

        await svc.SaveAsync(config);
        var loaded = await svc.LoadAsync();

        Assert.Equal(ProviderType.Cerebras, loaded.DefaultProvider);
        Assert.Equal("test-key", loaded.Cerebras.ApiKey);
    }

    [Fact]
    public async Task Set_UpdatesProvider()
    {
        var svc = CreateService();
        await svc.SetAsync("defaultProvider", "cerebras");
        var config = await svc.LoadAsync();

        Assert.Equal(ProviderType.Cerebras, config.DefaultProvider);
    }

    [Fact]
    public async Task Set_UpdatesOllamaModel()
    {
        var svc = CreateService();
        await svc.SetAsync("ollama.model", "gpt-oss:20b");
        Assert.Equal("gpt-oss:20b", await svc.GetAsync("ollama.model"));
    }

    [Fact]
    public async Task Set_InvalidProvider_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("defaultProvider", "invalid"));
    }

    [Fact]
    public async Task Set_UnknownKey_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("unknown.key", "value"));
    }

    [Fact]
    public async Task Get_ApiKey_ReturnsMasked()
    {
        var svc = CreateService();
        await svc.SetAsync("cerebras.apiKey", "secret");
        var result = await svc.GetAsync("cerebras.apiKey");
        Assert.Equal("***", result);
    }

    [Fact]
    public async Task Get_ApiKey_ReturnsNotSet_WhenEmpty()
    {
        var svc = CreateService();
        var result = await svc.GetAsync("cerebras.apiKey");
        Assert.Equal("(nicht gesetzt)", result);
    }

    [Fact]
    public async Task List_ContainsAllKeys()
    {
        var svc = CreateService();
        var list = await svc.ListAsync();

        Assert.Contains("defaultProvider", list);
        Assert.Contains("ollama.baseUrl", list);
        Assert.Contains("ollama.model", list);
        Assert.Contains("ollama.temperature", list);
        Assert.Contains("ollama.topP", list);
        Assert.Contains("ollama.numCtx", list);
        Assert.Contains("ollama.numPredict", list);
        Assert.Contains("ollama.repeatPenalty", list);
        Assert.Contains("ollama.seed", list);
        Assert.Contains("cerebras.apiKey", list);
        Assert.Contains("cerebras.model", list);
        Assert.Contains("cerebras.temperature", list);
        Assert.Contains("cerebras.topP", list);
        Assert.Contains("cerebras.maxCompletionTokens", list);
        Assert.Contains("cerebras.seed", list);
        Assert.Contains("cerebras.reasoningEffort", list);
    }

    [Fact]
    public async Task Set_CerebrasAdvancedOptions_ArePersisted()
    {
        var svc = CreateService();
        await svc.SetAsync("cerebras.temperature", "0.3");
        await svc.SetAsync("cerebras.topP", "0.85");
        await svc.SetAsync("cerebras.maxCompletionTokens", "4096");
        await svc.SetAsync("cerebras.seed", "42");
        await svc.SetAsync("cerebras.reasoningEffort", "high");

        var loaded = await svc.LoadAsync();
        Assert.Equal(0.3, loaded.Cerebras.Temperature);
        Assert.Equal(0.85, loaded.Cerebras.TopP);
        Assert.Equal(4096, loaded.Cerebras.MaxCompletionTokens);
        Assert.Equal(42, loaded.Cerebras.Seed);
        Assert.Equal("high", loaded.Cerebras.ReasoningEffort);
    }

    [Fact]
    public async Task Set_OllamaAdvancedOptions_ArePersisted()
    {
        var svc = CreateService();
        await svc.SetAsync("ollama.temperature", "0.4");
        await svc.SetAsync("ollama.topP", "0.9");
        await svc.SetAsync("ollama.numCtx", "32768");
        await svc.SetAsync("ollama.numPredict", "512");
        await svc.SetAsync("ollama.repeatPenalty", "1.1");
        await svc.SetAsync("ollama.seed", "77");

        var loaded = await svc.LoadAsync();
        Assert.Equal(0.4, loaded.Ollama.Temperature);
        Assert.Equal(0.9, loaded.Ollama.TopP);
        Assert.Equal(32768, loaded.Ollama.NumCtx);
        Assert.Equal(512, loaded.Ollama.NumPredict);
        Assert.Equal(1.1, loaded.Ollama.RepeatPenalty);
        Assert.Equal(77, loaded.Ollama.Seed);
    }

    [Fact]
    public async Task Load_NormalizesNullProviderOptions_ToDefaults()
    {
        var svc = CreateService();
        var config = new AppConfig
        {
            Ollama = new OllamaConfig
            {
                BaseUrl = "http://localhost:11434",
                Model = "gpt-oss:20b",
                Temperature = null,
                TopP = null,
                NumCtx = null,
                NumPredict = null,
                RepeatPenalty = null,
                Seed = null,
            },
            Cerebras = new CerebrasConfig
            {
                Model = "gpt-oss:120b-cloud",
                BaseUrl = "https://api.cerebras.ai/v1",
                Temperature = null,
                TopP = null,
                MaxCompletionTokens = null,
                Seed = null,
                ReasoningEffort = null,
            }
        };

        await svc.SaveAsync(config);
        var loaded = await svc.LoadAsync();

        Assert.Equal(0.2, loaded.Ollama.Temperature);
        Assert.Equal(0.9, loaded.Ollama.TopP);
        Assert.Equal(16384, loaded.Ollama.NumCtx);
        Assert.Equal(1024, loaded.Ollama.NumPredict);
        Assert.Equal(1.05, loaded.Ollama.RepeatPenalty);
        Assert.Null(loaded.Ollama.Seed);

        Assert.Equal(0.2, loaded.Cerebras.Temperature);
        Assert.Equal(0.9, loaded.Cerebras.TopP);
        Assert.Equal(2048, loaded.Cerebras.MaxCompletionTokens);
        Assert.Equal("medium", loaded.Cerebras.ReasoningEffort);
        Assert.Null(loaded.Cerebras.Seed);
    }
}

/// <summary>
/// Testbare Variante, die ein temp. Verzeichnis statt ~/.config/bashgpt nutzt.
/// </summary>
internal class TestableConfigurationService(string configDir) : ConfigurationService
{
    protected override string ConfigFile { get; } =
        Path.Combine(configDir, "config.json");
}
