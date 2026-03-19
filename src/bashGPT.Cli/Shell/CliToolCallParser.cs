using System.Text.Json;
using bashGPT.Core.Models.Providers;

namespace bashGPT.Cli.Shell;

internal static class CliToolCallParser
{
    public static bool TryGetCommand(ToolCall call, out string command, out string? error)
    {
        command = "";
        error = null;

        if (!call.Name.Equals("bash", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unknown tool '{call.Name}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(call.ArgumentsJson))
        {
            error = "Empty tool arguments.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(call.ArgumentsJson);
            if (!doc.RootElement.TryGetProperty("command", out var cmdEl))
            {
                error = "Tool arguments do not contain a 'command' field.";
                return false;
            }

            if (cmdEl.ValueKind != JsonValueKind.String)
            {
                error = "Tool argument 'command' is not a string.";
                return false;
            }

            command = cmdEl.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(command))
            {
                error = "Tool argument 'command' is empty.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON in tool arguments: {ex.Message}";
            return false;
        }
    }
}
