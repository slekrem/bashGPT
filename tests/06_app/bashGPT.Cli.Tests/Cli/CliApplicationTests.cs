using bashGPT.Core.Configuration;
using bashGPT.Cli;

namespace bashGPT.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public void CreateConfigurationService_ReturnsConfigurationService()
    {
        var configService = CliApplication.CreateConfigurationService();

        Assert.NotNull(configService);
        Assert.IsType<ConfigurationService>(configService);
    }

    [Fact]
    public void CreateChatRunner_ReturnsCliChatRunner()
    {
        var configService = new TestConfigurationService(Path.GetTempFileName());

        var runner = CliApplication.CreateChatRunner(configService);

        Assert.NotNull(runner);
        Assert.IsType<CliChatRunner>(runner);
    }
}
