using BashGPT.Configuration;

namespace BashGPT.Tests.Server;

internal sealed class TestConfigurationService(string path) : ConfigurationService
{
    protected override string ConfigFile => path;
}
