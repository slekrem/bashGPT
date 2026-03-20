using System.Reflection;
using System.Runtime.Loader;

namespace bashGPT.Plugins;

/// <summary>
/// Isolated <see cref="AssemblyLoadContext"/> for a single plugin DLL.
/// Each plugin subdirectory gets its own context so that plugin-private
/// dependencies do not interfere with each other or with the host process.
/// </summary>
/// <remarks>
/// Shared contract assemblies (e.g. bashGPT.Tools, bashGPT.Agents) that are
/// already present in the default context are intentionally NOT reloaded here.
/// Returning <see langword="null"/> from <see cref="Load"/> makes the runtime
/// fall back to the default context, which ensures that <c>ITool</c> and
/// <c>AgentBase</c> instances from plugins are the same type objects as those
/// used by the host — a prerequisite for the <c>is</c> / <c>IsAssignableFrom</c>
/// checks performed by <see cref="PluginLoader"/>.
/// </remarks>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string mainPluginDllPath) : base(isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainPluginDllPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // If the assembly is already loaded in the default context,
        // return null so the shared version is used.
        // This prevents duplicate type identities for bashGPT.Tools, bashGPT.Agents, etc.
        foreach (var loaded in Default.Assemblies)
        {
            if (string.Equals(loaded.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
