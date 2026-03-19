using bashGPT.Core.Models.Providers;

namespace bashGPT.Cli.Shell;

internal static class CliBashTool
{
    public static readonly ToolDefinition Definition = new(
        Name: "bash",
        Description: "Executes a shell command",
        Parameters: new
        {
            type = "object",
            properties = new
            {
                command = new
                {
                    type = "string",
                    description = "Shell command"
                }
            },
            required = new[] { "command" }
        });
}
