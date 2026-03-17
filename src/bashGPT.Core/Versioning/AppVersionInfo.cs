using System.Reflection;

namespace BashGPT.Versioning;

public sealed record AppVersionInfo(
    string Application,
    string Version,
    string InformationalVersion,
    string? RepositoryUrl);

public static class AppVersion
{
    public static AppVersionInfo ForAssembly(Assembly assembly)
    {
        var name = assembly.GetName();
        var application = name.Name ?? "bashGPT";
        var version = name.Version?.ToString() ?? "0.0.0.0";
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? version;
        var repositoryUrl = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;

        return new AppVersionInfo(application, version, informationalVersion, repositoryUrl);
    }
}
