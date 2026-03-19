using bashGPT.Core.Configuration;

namespace bashGPT.Cli;

internal static class CliApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static CliChatRunner CreateChatRunner(ConfigurationService configService) =>
        new(configService);
}
