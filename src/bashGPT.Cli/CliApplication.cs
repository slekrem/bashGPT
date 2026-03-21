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
    /// Scans the plugin directory and returns all discovered tools and agents.
    /// Non-fatal loading errors are written to <see cref="Console.Error"/>.
    /// </summary>
    public static PluginLoadResult LoadPlugins(string? pluginDir = null)
    {
        var dir = pluginDir ?? AppBootstrap.GetPluginsDir();
        var result = PluginLoader.LoadFromDirectory(dir);

        foreach (var error in result.Errors)
            Console.Error.WriteLine($"[plugin] {Path.GetFileName(error.Source)}: {error.Message}");

        // The CLI has no agent-selection model, so plugin agents cannot be activated.
        if (result.Agents.Count > 0)
            Console.Error.WriteLine(
                $"[plugin] {result.Agents.Count} agent(s) discovered but ignored — agents are only supported by the server.");

        return result;
    }
}
