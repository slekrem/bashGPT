using System.Text.Json;

namespace bashGPT.Core.Models.Providers;

public static class ToolCallArguments
{
    public static bool TryGetString(ToolCall call, string propertyName, out string value, out string? error)
    {
        value = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(call.ArgumentsJson))
        {
            error = "Empty tool arguments.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(call.ArgumentsJson);
            if (!doc.RootElement.TryGetProperty(propertyName, out var element))
            {
                error = $"Tool arguments do not contain a '{propertyName}' field.";
                return false;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                error = $"Tool argument '{propertyName}' is not a string.";
                return false;
            }

            value = element.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"Tool argument '{propertyName}' is empty.";
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
