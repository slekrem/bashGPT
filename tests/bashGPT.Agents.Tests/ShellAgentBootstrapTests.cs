using BashGPT.Agents.Shell;

namespace BashGPT.Agents.Tests;

public class ShellAgentBootstrapTests
{
    [Fact]
    public void ShellAgent_HasExpectedDefaults()
    {
        var shell = ShellAgentBootstrap.ShellAgent;

        Assert.Equal("shell", shell.Id);
        Assert.Equal("Shell-Agent", shell.Name);
        Assert.Single(shell.EnabledTools);
        Assert.Contains("shell_exec", shell.EnabledTools);
    }
}
