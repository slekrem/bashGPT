using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace MyPlugin;

/// <summary>
/// Example tool that echoes its input back to the model.
/// Drop this DLL into ~/.config/bashgpt/plugins/MyPlugin/ to activate it.
/// </summary>
public sealed class EchoTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "echo",
        Description: "Echoes the given message back to the model. Useful for testing plugin loading.",
        Parameters:
        [
            new ToolParameter(
                Name: "message",
                Type: "string",
                Description: "The message to echo.",
                Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        string message;
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(call.ArgumentsJson);
            message = args.GetProperty("message").GetString() ?? "(empty)";
        }
        catch
        {
            return Task.FromResult(new ToolResult(false, "Error: could not parse arguments."));
        }

        return Task.FromResult(new ToolResult(true, $"Echo: {message}"));
    }
}
