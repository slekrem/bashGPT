namespace BashGPT.Tools.Filesystem;

/// <summary>
/// Erlaubt Read/Write nur innerhalb konfigurierter Root-Verzeichnisse.
/// Ohne konfigurierte Roots ist nur das aktuelle Arbeitsverzeichnis erlaubt.
/// </summary>
public sealed class DefaultFilesystemPolicy : IFilesystemPolicy
{
    private readonly IReadOnlyList<string> _allowedRoots;

    public DefaultFilesystemPolicy(IEnumerable<string>? allowedRoots = null)
    {
        var roots = allowedRoots?.Select(Path.GetFullPath).ToList()
            ?? [Directory.GetCurrentDirectory()];
        _allowedRoots = roots;
    }

    public bool AllowRead(string absolutePath) => IsWithinAllowedRoot(absolutePath);
    public bool AllowWrite(string absolutePath) => IsWithinAllowedRoot(absolutePath);

    private bool IsWithinAllowedRoot(string absolutePath)
    {
        var normalized = Path.GetFullPath(absolutePath);
        return _allowedRoots.Any(root =>
            normalized.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(root, StringComparison.OrdinalIgnoreCase));
    }
}
