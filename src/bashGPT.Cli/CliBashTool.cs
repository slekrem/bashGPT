namespace BashGPT.Cli;

internal static class CliBashTool
{
    public static readonly bashGPT.Core.Providers.ToolDefinition Definition = new(
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
