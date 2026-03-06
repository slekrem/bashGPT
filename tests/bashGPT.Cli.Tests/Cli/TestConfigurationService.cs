using BashGPT.Configuration;

namespace BashGPT.Cli.Tests;

internal sealed class TestConfigurationService(string configFile) : ConfigurationService
{
    protected override string ConfigFile => configFile;
}
