using bashGPT.Core.Configuration;

namespace BashGPT.Core.Tests.Configuration;

public class ConfigurationServiceTests : IDisposable
{
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

        Assert.False(config.DefaultForceTools);
        Assert.Equal("http://localhost:11434", config.Ollama.BaseUrl);
        Assert.Equal("gpt-oss:20b", config.Ollama.Model);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var svc = CreateService();
        var config = new AppConfig
        {
            DefaultForceTools = true,
            Ollama = new OllamaConfig { Model = "llama3.2", BaseUrl = "http://ollama.local:11434" }
        };

        await svc.SaveAsync(config);
        var loaded = await svc.LoadAsync();

        Assert.True(loaded.DefaultForceTools);
        Assert.Equal("llama3.2", loaded.Ollama.Model);
        Assert.Equal("http://ollama.local:11434", loaded.Ollama.BaseUrl);
    }

    [Fact]
    public async Task Set_Provider_Throws()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("defaultProvider", "ollama"));
        Assert.Contains("obsolete", ex.Message);
    }

    [Fact]
    public async Task Set_UpdatesOllamaModel()
    {
        var svc = CreateService();
        await svc.SetAsync("ollama.model", "gpt-oss:20b");
        Assert.Equal("gpt-oss:20b", await svc.GetAsync("ollama.model"));
    }

    [Fact]
    public async Task Get_Provider_ReturnsOllama()
    {
        var svc = CreateService();
        Assert.Equal("ollama", await svc.GetAsync("provider"));
    }

    [Fact]
    public async Task Set_UnknownKey_Throws()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("unknown.key", "value"));
        Assert.Contains("Unknown configuration key", ex.Message);
    }

    [Fact]
    public async Task Set_RemovedProviderKey_Throws()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("removed-provider.apiKey", "secret"));
        Assert.Contains("Unknown configuration key", ex.Message);
    }

    [Fact]
    public async Task Set_ExecMode_Throws()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("execMode", "auto-exec"));
        Assert.Contains("Unknown configuration key", ex.Message);
    }

    [Fact]
    public async Task Get_ExecMode_Throws()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.GetAsync("execMode"));
        Assert.Contains("Unknown configuration key", ex.Message);
    }

    [Fact]
    public async Task Set_ForceTools_InvalidValue_ThrowsEnglishError()
    {
        var svc = CreateService();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.SetAsync("forceTools", "yes"));
        Assert.Contains("Invalid value for 'forceTools'", ex.Message);
    }

    [Fact]
    public async Task Load_InvalidJson_ThrowsEnglishError()
    {
        var svc = CreateService();
        await File.WriteAllTextAsync(Path.Combine(_tmpDir, "config.json"), "{ invalid json");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.LoadAsync());

        Assert.Contains("Configuration file", ex.Message);
        Assert.Contains("is invalid", ex.Message);
    }

    [Fact]
    public async Task SetAsync_ForceTools_PersistsValue()
    {
        var svc = CreateService();
        await svc.SetAsync("forceTools", "true");
        var config = await svc.LoadAsync();
        Assert.True(config.DefaultForceTools);
    }

    [Fact]
    public async Task List_ContainsSupportedKeys()
    {
        var svc = CreateService();
        var list = await svc.ListAsync();

        Assert.Contains("provider", list);
        Assert.Contains("forceTools", list);
        Assert.Contains("ollama.baseUrl", list);
        Assert.Contains("ollama.model", list);
        Assert.DoesNotContain("defaultProvider", list);
        Assert.DoesNotContain("execMode", list);
    }

    [Fact]
    public async Task ApplyEnvironmentOverrides_BASHGPT_FORCE_TOOLS_AppliesCorrectly()
    {
        var svc = CreateService();
        Environment.SetEnvironmentVariable("BASHGPT_FORCE_TOOLS", "true");
        try
        {
            var config = await svc.LoadAsync();
            Assert.True(config.DefaultForceTools);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BASHGPT_FORCE_TOOLS", null);
        }
    }
}

internal class TestableConfigurationService(string configDir) : ConfigurationService
{
    protected override string ConfigFile { get; } =
        Path.Combine(configDir, "config.json");
}
