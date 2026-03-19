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

        return ToolCallArguments.TryGetString(call, "command", out command, out error);
    }
}
