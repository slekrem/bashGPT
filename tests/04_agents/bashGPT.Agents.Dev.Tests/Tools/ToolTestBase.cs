using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tests.Tools;

/// <summary>
/// Shared setup for DevAgent tool tests: temporary working directory
/// that is cleaned up after each test class.
/// </summary>
public abstract class ToolTestBase : IDisposable
{
    protected readonly string Dir;

    protected ToolTestBase()
    {
        Dir = Path.Combine(Path.GetTempPath(), $"bashgpt-{GetType().Name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Dir);
    }

    public void Dispose() => Directory.Delete(Dir, recursive: true);

    protected void WriteFile(string relativePath, string content)
    {
        var full = Path.Combine(Dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    protected static ToolCall Call(string name, string json) =>
        new(name, json, SessionPath: null);
}
