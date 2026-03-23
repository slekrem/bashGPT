using bashGPT.Core.Versioning;

namespace bashGPT.Server;

internal sealed class VersionApiHandler
{
    public async Task HandleAsync(HttpContext ctx, CancellationToken ct)
    {
        var info = AppVersion.ForAssembly(typeof(VersionApiHandler).Assembly);
        await ctx.Response.WriteJsonAsync(new
        {
            application = info.Application,
            version = info.Version,
            informationalVersion = info.InformationalVersion,
            repositoryUrl = info.RepositoryUrl,
        });
    }
}
