using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry)
{
    public IResult Get()
    {
        if (toolRegistry is null)
            return Results.Json(new { tools = Array.Empty<object>() });

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

        return Results.Json(new { tools });
    }
}
