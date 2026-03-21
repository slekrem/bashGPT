using bashGPT.Plugins.TestFixtures;

namespace bashGPT.Plugins.Tests;

public sealed class PluginLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void LoadFromDirectory_NonExistentDirectory_ReturnsEmptyResult()
    {
        var result = PluginLoader.LoadFromDirectory(Path.Combine(_tempDir, "does-not-exist"));

        Assert.Empty(result.Tools);
        Assert.Empty(result.Agents);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LoadFromDirectory_EmptyDirectory_ReturnsEmptyResult()
    {
        Directory.CreateDirectory(_tempDir);

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.Empty(result.Tools);
        Assert.Empty(result.Agents);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LoadFromDirectory_SubdirectoryWithNoDll_RecordsError()
    {
        var pluginDir = Path.Combine(_tempDir, "EmptyPlugin");
        Directory.CreateDirectory(pluginDir);

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.Empty(result.Tools);
        Assert.Empty(result.Agents);
        Assert.Single(result.Errors);
        Assert.Contains("No DLL found", result.Errors[0].Message);
    }

    [Fact]
    public void LoadFromDirectory_AssemblyWithIToolImplementation_ReturnsTools()
    {
        CopyFixtureAssembly();

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.Contains(result.Tools, t => t.Definition.Name == "fake_tool");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LoadFromDirectory_AssemblyWithAgentBaseSubclass_ReturnsAgents()
    {
        CopyFixtureAssembly();

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.Contains(result.Agents, a => a.Id == "fake-agent");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LoadFromDirectory_AssemblyWithBothToolsAndAgents_ReturnsBoth()
    {
        CopyFixtureAssembly();

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.NotEmpty(result.Tools);
        Assert.NotEmpty(result.Agents);
    }

    [Fact]
    public void LoadFromDirectory_InvalidDll_RecordsErrorAndContinues()
    {
        var pluginDir = Path.Combine(_tempDir, "BrokenPlugin");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllBytes(Path.Combine(pluginDir, "BrokenPlugin.dll"), [0x00, 0x01, 0x02, 0x03]);

        // A second, valid plugin loaded after the broken one.
        CopyFixtureAssembly();

        var result = PluginLoader.LoadFromDirectory(_tempDir);

        Assert.NotEmpty(result.Errors);
        // The valid fixture plugin must still be loaded.
        Assert.NotEmpty(result.Tools);
    }

    // Copies the TestFixtures assembly into a subdirectory matching the discovery convention.
    private void CopyFixtureAssembly()
    {
        var fixtureDllPath = typeof(FakeToolFixture).Assembly.Location;
        var fixtureDir = Path.GetDirectoryName(fixtureDllPath)!;
        var pluginName = Path.GetFileNameWithoutExtension(fixtureDllPath);
        var pluginDir = Path.Combine(_tempDir, pluginName);
        Directory.CreateDirectory(pluginDir);

        // Copy the fixture DLL and all its dependencies so that AssemblyDependencyResolver
        // can satisfy transitive loads (e.g. bashGPT.Tools, bashGPT.Agents).
        foreach (var file in Directory.GetFiles(fixtureDir, "*.dll"))
            File.Copy(file, Path.Combine(pluginDir, Path.GetFileName(file)), overwrite: true);

        // Copy the .deps.json so AssemblyDependencyResolver works correctly.
        var depsJson = Path.Combine(fixtureDir, pluginName + ".deps.json");
        if (File.Exists(depsJson))
            File.Copy(depsJson, Path.Combine(pluginDir, pluginName + ".deps.json"), overwrite: true);
    }
}
