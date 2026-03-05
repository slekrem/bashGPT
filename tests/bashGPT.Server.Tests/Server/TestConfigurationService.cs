using BashGPT.Configuration;

namespace BashGPT.Server.Tests;

internal sealed class TestConfigurationService(string path) : ConfigurationService
{
    protected override string ConfigFile => path;
}
