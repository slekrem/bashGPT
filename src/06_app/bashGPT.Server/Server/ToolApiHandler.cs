using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry)
{
    public async Task GetAsync(HttpResponse response, CancellationToken ct)
    {
        if (toolRegistry is null)
        {
            await response.WriteJsonAsync(new { tools = Array.Empty<object>() });
            return;
        }

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

        await response.WriteJsonAsync(new { tools });
    }
}
