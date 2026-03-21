using System.Reflection;
using bashGPT.Agents;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Plugins;

/// <summary>
/// Scans a plugin directory for external tool and agent implementations.
/// </summary>
/// <remarks>
/// <para><b>Directory layout:</b></para>
/// <code>
/// ~/.config/bashgpt/plugins/
///   MyPlugin/
///     MyPlugin.dll       ← main plugin assembly (name must match directory)
///     SomeDependency.dll ← plugin-private dependency (optional)
///   AnotherPlugin/
///     AnotherPlugin.dll
/// </code>
/// <para>
/// Each subdirectory of the plugin root is treated as one plugin.
/// The main DLL must match the subdirectory name (e.g. <c>MyPlugin/MyPlugin.dll</c>).
/// If no matching DLL is found, the first <c>*.dll</c> in the directory is used as a fallback.
/// </para>
/// <para><b>Discovery rules:</b></para>
/// <list type="bullet">
///   <item>Public, non-abstract classes implementing <see cref="ITool"/> are instantiated as tools.</item>
///   <item>Public, non-abstract classes subclassing <see cref="AgentBase"/> are instantiated as agents.</item>
///   <item>Both require a public parameterless constructor.</item>
///   <item>Built-ins always win on name/ID collision — duplicate plugins are skipped with a warning.</item>
/// </list>
/// <para><b>Security note:</b></para>
/// <para>
/// Plugin assemblies are fully trusted — they run in the same process as the host with no sandboxing.
/// Only load plugins from sources you control.
/// </para>
/// </remarks>
public static class PluginLoader
{
    /// <summary>
    /// Scans multiple plugin directories and merges all discovered tools, agents, and errors.
    /// Directories that do not exist are silently skipped.
    /// </summary>
    public static PluginLoadResult LoadFromDirectories(IEnumerable<string> pluginDirectories)
    {
        var tools = new List<ITool>();
        var agents = new List<AgentBase>();
        var errors = new List<PluginLoadError>();

        foreach (var dir in pluginDirectories)
        {
            var result = LoadFromDirectory(dir);
            tools.AddRange(result.Tools);
            agents.AddRange(result.Agents);
            errors.AddRange(result.Errors);
        }

        return new PluginLoadResult(tools, agents, errors);
    }

    /// <summary>
    /// Scans <paramref name="pluginDirectory"/> for plugin subdirectories and returns all
    /// discovered tools and agents together with any non-fatal loading errors.
    /// </summary>
    /// <param name="pluginDirectory">
    /// Root directory that contains one subdirectory per plugin.
    /// Returns <see cref="PluginLoadResult.Empty"/> if the directory does not exist.
    /// </param>
    public static PluginLoadResult LoadFromDirectory(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            return PluginLoadResult.Empty;

        var tools = new List<ITool>();
        var agents = new List<AgentBase>();
        var errors = new List<PluginLoadError>();

        foreach (var pluginDir in Directory.GetDirectories(pluginDirectory))
        {
            var pluginName = Path.GetFileName(pluginDir);

            // Convention: main DLL name matches subdirectory name.
            var mainDll = Path.Combine(pluginDir, pluginName + ".dll");
            if (!File.Exists(mainDll))
            {
                // Fallback: first DLL found in the directory.
                var dlls = Directory.GetFiles(pluginDir, "*.dll");
                if (dlls.Length == 0)
                {
                    errors.Add(new PluginLoadError(pluginDir, "No DLL found in plugin directory."));
                    continue;
                }

                mainDll = dlls[0];
            }

            LoadPlugin(mainDll, tools, agents, errors);
        }

        return new PluginLoadResult(tools, agents, errors);
    }

    private static void LoadPlugin(
        string dllPath,
        List<ITool> tools,
        List<AgentBase> agents,
        List<PluginLoadError> errors)
    {
        Assembly assembly;
        try
        {
            var context = new PluginLoadContext(dllPath);
            assembly = context.LoadPluginAssembly(dllPath);
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(dllPath, $"Failed to load assembly: {ex.Message}"));
            return;
        }

        Type[] exportedTypes;
        try
        {
            exportedTypes = assembly.GetExportedTypes();
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(dllPath, $"Failed to inspect types: {ex.Message}"));
            return;
        }

        foreach (var type in exportedTypes)
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            if (typeof(ITool).IsAssignableFrom(type))
                TryInstantiate<ITool>(type, dllPath, tools, errors);
            else if (typeof(AgentBase).IsAssignableFrom(type))
                TryInstantiate<AgentBase>(type, dllPath, agents, errors);
        }
    }

    private static void TryInstantiate<T>(
        Type type,
        string dllPath,
        List<T> list,
        List<PluginLoadError> errors)
        where T : class
    {
        try
        {
            var ctor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().All(p => p.HasDefaultValue));

            if (ctor is null)
            {
                errors.Add(new PluginLoadError(dllPath,
                    $"Type '{type.FullName}' has no callable constructor (all parameters must have default values)."));
                return;
            }

            var args = ctor.GetParameters().Select(p => p.DefaultValue).ToArray();
            if (ctor.Invoke(args) is T instance)
                list.Add(instance);
            else
                errors.Add(new PluginLoadError(dllPath,
                    $"Type '{type.FullName}' could not be cast to {typeof(T).Name}."));
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(dllPath,
                $"Failed to instantiate '{type.FullName}': {ex.Message}"));
        }
    }
}
