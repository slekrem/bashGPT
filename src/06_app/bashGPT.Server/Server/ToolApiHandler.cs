using System.Net;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry, ServerToolSelectionPolicy toolSelectionPolicy)
{
    public async Task HandleAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (toolRegistry is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { tools = Array.Empty<object>() });
            return;
        }

        var tools = toolRegistry.Tools
            .Where(IsSelectable)
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

    private bool IsSelectable(ITool tool) => toolSelectionPolicy.IsAllowed(tool.Definition.Name);
}
