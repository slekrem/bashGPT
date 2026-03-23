using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry)
{
    public async Task HandleAsync(HttpContext ctx, CancellationToken ct)
    {
        if (toolRegistry is null)
        {
            await ctx.Response.WriteJsonAsync(new { tools = Array.Empty<object>() });
            return;
        }

        var visibleTools = toolRegistry.Tools;

        var tools = visibleTools
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

        await ctx.Response.WriteJsonAsync(new { tools });
    }
}
