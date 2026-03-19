using System.Net;
using bashGPT.Core.Versioning;

namespace bashGPT.Server;

internal sealed class VersionApiHandler
{
    public async Task HandleAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var info = AppVersion.ForAssembly(typeof(ServerHost).Assembly);
        await ApiResponse.WriteJsonAsync(response, new
        {
            application = info.Application,
            version = info.Version,
            informationalVersion = info.InformationalVersion,
            repositoryUrl = info.RepositoryUrl,
        });
    }
}
