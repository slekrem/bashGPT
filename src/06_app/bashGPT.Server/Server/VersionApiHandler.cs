using bashGPT.Core.Versioning;

namespace bashGPT.Server;

internal sealed class VersionApiHandler
{
    public async Task GetAsync(HttpResponse response, CancellationToken ct)
    {
        var info = AppVersion.ForAssembly(typeof(VersionApiHandler).Assembly);
        await response.WriteJsonAsync(new
        {
            application = info.Application,
            version = info.Version,
            informationalVersion = info.InformationalVersion,
            repositoryUrl = info.RepositoryUrl,
        });
    }
}
