using bashGPT.Core.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/version")]
public sealed class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var info = AppVersion.ForAssembly(typeof(VersionController).Assembly);
        return Ok(new
        {
            application = info.Application,
            version = info.Version,
            informationalVersion = info.InformationalVersion,
            repositoryUrl = info.RepositoryUrl,
        });
    }
}
