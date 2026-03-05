using System.Net;
using BashGPT.Tools.Execution;

namespace BashGPT.Server;

internal sealed class ToolApiHandler(ToolRegistry? toolRegistry)
{
    public async Task HandleAsync(HttpListenerResponse response, CancellationToken ct)
    {
        if (toolRegistry is null)
        {
            await ApiResponse.WriteJsonAsync(response, new { tools = Array.Empty<object>() });
            return;
        }

        var tools = toolRegistry.Tools.Select(t => new
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
