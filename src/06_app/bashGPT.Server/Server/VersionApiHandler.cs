using bashGPT.Core.Versioning;

namespace bashGPT.Server;

internal sealed class VersionApiHandler
{
    public IResult Get()
    {
        var info = AppVersion.ForAssembly(typeof(VersionApiHandler).Assembly);
        return Results.Json(new
        {
            application = info.Application,
            version = info.Version,
            informationalVersion = info.InformationalVersion,
            repositoryUrl = info.RepositoryUrl,
        });
    }
}
