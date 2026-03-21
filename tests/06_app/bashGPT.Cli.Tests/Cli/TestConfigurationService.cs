using bashGPT.Core.Configuration;

namespace bashGPT.Cli.Tests;

internal sealed class TestConfigurationService(string configFile) : ConfigurationService
{
    protected override string ConfigFile => configFile;
}
