using System.Net;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry, ServerToolSelectionPolicy? selectionPolicy = null)
{
    public async Task HandleAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (toolRegistry is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { tools = Array.Empty<object>() });
            return;
        }

        var allTools = toolRegistry.Tools;
        var visibleTools = selectionPolicy is null
            ? allTools
            : allTools.Where(t => selectionPolicy.IsAllowed(t.Definition.Name));

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

        await ApiResponse.WriteJsonAsync(response, new { tools });
    }
}
