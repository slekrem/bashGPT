using BashGPT.Configuration;
using BashGPT.Shell;

namespace BashGPT.Core.Tests.Configuration;

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
        Assert.Equal("gpt-oss:120b-cloud", config.Cerebras.Model);
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
        Assert.Contains("cerebras.apiKey", list);
        Assert.Contains("cerebras.model", list);
        Assert.Contains("cerebras.baseUrl", list);
    }

    [Fact]
    public async Task SetAsync_CommandTimeoutSeconds_PersistsValue()
    {
        var svc = CreateService();
        await svc.SetAsync("commandTimeoutSeconds", "120");
        var config = await svc.LoadAsync();
        Assert.Equal(120, config.CommandTimeoutSeconds);
    }

    [Fact]
    public async Task SetAsync_ExecMode_PersistsValue()
    {
        var svc = CreateService();
        await svc.SetAsync("execMode", "auto-exec");
        var config = await svc.LoadAsync();
        Assert.Equal(ExecutionMode.AutoExec, config.DefaultExecMode);
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
    public async Task GetAsync_ExecMode_ReturnsKebabCaseString()
    {
        var svc = CreateService();
        await svc.SetAsync("execMode", "dry-run");
        var result = await svc.GetAsync("execMode");
        Assert.Equal("dry-run", result);
    }

    [Fact]
    public async Task ListAsync_IncludesAllNewKeys()
    {
        var svc = CreateService();
        var list = await svc.ListAsync();
        Assert.Contains("commandTimeoutSeconds", list);
        Assert.Contains("execMode", list);
        Assert.Contains("forceTools", list);
        Assert.Contains("loopDetectionEnabled", list);
        Assert.Contains("maxToolCallRounds", list);
    }

    [Fact]
    public async Task ApplyEnvironmentOverrides_BASHGPT_EXEC_MODE_AppliesCorrectly()
    {
        var svc = CreateService();
        Environment.SetEnvironmentVariable("BASHGPT_EXEC_MODE", "no-exec");
        try
        {
            var config = await svc.LoadAsync();
            Assert.Equal(ExecutionMode.NoExec, config.DefaultExecMode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BASHGPT_EXEC_MODE", null);
        }
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
