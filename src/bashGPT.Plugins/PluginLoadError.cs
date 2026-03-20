namespace bashGPT.Plugins;

/// <summary>
/// Describes a single error that occurred while loading a plugin.
/// </summary>
/// <param name="Source">File system path of the DLL or plugin directory that caused the error.</param>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PluginLoadError(string Source, string Message);
