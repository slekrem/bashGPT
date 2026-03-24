using bashGPT.Tools.Registration;
using Microsoft.AspNetCore.Mvc;

namespace bashGPT.Server.Controllers;

[ApiController]
[Route("api/tools")]
public sealed class ToolsController(ToolRegistry? toolRegistry) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        if (toolRegistry is null)
            return Ok(new { tools = Array.Empty<object>() });

        var tools = toolRegistry.Tools
            .Select(t => new
            {
                name        = t.Definition.Name,
                description = t.Definition.Description,
                parameters  = t.Definition.Parameters.Select(p => new
                {
                    name        = p.Name,
                    type        = p.Type,
                    description = p.Description,
                    required    = p.Required,
                }),
            });

        return Ok(new { tools });
    }
}
