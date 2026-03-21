using System.Runtime.InteropServices;
using bashGPT.Core;
using bashGPT.Core.Storage;

namespace bashGPT.Core.Tests;

public class AppBootstrapTests
{
    [Fact]
    public void GetDefaultConfigDir_ReturnsExpectedPlatformPath()
    {
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bashgpt")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "bashgpt");

        var actual = AppBootstrap.GetDefaultConfigDir();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetSessionsDir_AppendsSessionsFolder()
    {
        var configDir = Path.Combine("root", "bashgpt-config");
        var actual = AppBootstrap.GetSessionsDir(configDir);

        Assert.Equal(Path.Combine(configDir, "sessions"), actual);
    }

    [Fact]
    public void GetDefaultConfigFilePath_AppendsConfigJson()
    {
        var actual = AppBootstrap.GetDefaultConfigFilePath();

        Assert.Equal(
            Path.Combine(AppBootstrap.GetDefaultConfigDir(), "config.json"),
            actual);
    }

    [Fact]
    public void CreateSessionStore_UsesResolvedSessionsDirectory()
    {
        var configDir = Path.Combine("root", "bashgpt-config");
        var store = AppBootstrap.CreateSessionStore(configDir);

        Assert.IsType<SessionStore>(store);
        Assert.Equal(
            Path.Combine(configDir, "sessions", SessionStore.LiveSessionId),
            store.GetSessionDir(SessionStore.LiveSessionId));
    }

    [Fact]
    public void CreateSessionRequestStore_UsesResolvedSessionsDirectory()
    {
        var configDir = Path.Combine("root", "bashgpt-config");
        var requestStore = AppBootstrap.CreateSessionRequestStore(configDir);

        Assert.IsType<SessionRequestStore>(requestStore);
    }
}
