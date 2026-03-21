using bashGPT.Core;
using bashGPT.Core.Configuration;
using bashGPT.Plugins;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Cli;

internal static class CliApplication
{
    public static ConfigurationService CreateConfigurationService() => new();

    public static CliChatRunner CreateChatRunner(
        ConfigurationService configService,
        IReadOnlyList<ITool>? pluginTools = null) =>
        new(configService, pluginTools);

    /// <summary>
    /// Scans the bundled plugins directory (next to the app binary) and the user config
    /// plugins directory, merges results, and reports non-fatal loading errors to stderr.
    /// </summary>
    public static PluginLoadResult LoadPlugins(string? userPluginDir = null)
    {
        var dirs = new[]
        {
            AppBootstrap.GetBundledPluginsDir(),
            userPluginDir ?? AppBootstrap.GetPluginsDir(),
        };

        var result = PluginLoader.LoadFromDirectories(dirs);

        foreach (var error in result.Errors)
            Console.Error.WriteLine($"[plugin] {Path.GetFileName(error.Source)}: {error.Message}");

        // The CLI has no agent-selection model, so plugin agents cannot be activated.
        if (result.Agents.Count > 0)
            Console.Error.WriteLine(
                $"[plugin] {result.Agents.Count} agent(s) discovered but ignored — agents are only supported by the server.");

        return result;
    }
}
