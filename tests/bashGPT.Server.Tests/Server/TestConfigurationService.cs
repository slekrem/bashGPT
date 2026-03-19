using bashGPT.Core.Configuration;

namespace bashGPT.Server.Tests;

internal sealed class TestConfigurationService(string path) : ConfigurationService
{
    protected override string ConfigFile => path;
}
