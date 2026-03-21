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
/// <para>
/// <b>Lifecycle:</b> This context is created with <c>isCollectible: false</c>.
/// Plugins are loaded once at startup and remain active until shutdown — there
/// is no need for unloading. Using a non-collectible context also avoids
/// restrictions that collectible contexts impose on certain assemblies.
/// </para>
/// </remarks>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    // Assemblies that must always come from the host to preserve type identity.
    // Plugins that physically ship copies of these DLLs must still use the host version.
    private static readonly HashSet<string> SharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bashGPT.Tools",
        "bashGPT.Agents",
        "bashGPT.Core",
    };

    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string mainPluginDllPath) : base(isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainPluginDllPath);
    }

    public Assembly LoadPluginAssembly(string mainPluginDllPath) =>
        LoadFromAssemblyPath(mainPluginDllPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Shared contracts must always come from the host to preserve type identity.
        // Returning null delegates resolution to the default context.
        if (SharedAssemblyNames.Contains(assemblyName.Name ?? string.Empty))
            return null;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
