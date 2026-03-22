# Plugin Authoring Guide

External tools and agents can be developed as standalone .NET assemblies and loaded into bashGPT without recompiling the host.

## Prerequisites

- .NET 9 SDK
- NuGet packages:
  - `bashGPT.Tools` — for external tools
  - `bashGPT.Agents` — for external agents (transitively includes `bashGPT.Tools`)

```xml
<PackageReference Include="bashGPT.Tools" Version="0.1.0" />
<!-- or for agents: -->
<PackageReference Include="bashGPT.Agents" Version="0.1.0" />
```

## Implementing a Tool

Implement `ITool` and do not take a dependency on the host — the plugin is loaded via reflection.

```csharp
using bashGPT.Tools.Abstractions;

public sealed class HelloTool : ITool
{
    public ToolDefinition Definition => new(
        Name:        "hello",
        Description: "Returns a greeting.",
        Parameters:  ToolParameters.Empty
    );

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var result = new ToolResult("Hello from the plugin!");
        return Task.FromResult(result);
    }
}
```

## Implementing an Agent

Subclass `AgentBase`, provide a stable `Id`, and define one or more `SystemPrompt` entries.

```csharp
using bashGPT.Agents;
using bashGPT.Tools.Abstractions;

public sealed class HelloAgent : AgentBase
{
    public override string Id   => "hello-agent";
    public override string Name => "Hello Agent";

    public override IReadOnlyList<ITool> GetOwnedTools() => [new HelloTool()];

    public override IReadOnlyList<string> SystemPrompt =>
    [
        "You are a friendly assistant that always starts with 'Hello!'."
    ];

    protected override string GetAgentMarkdown() =>
        "# Hello Agent\n\nA simple demo agent.";
}
```

Optional overrides:
- `LlmConfig` — custom model and sampling parameters (`AgentLlmConfig`)
- `EnabledTools` — additional registry tools by name
- `GetSystemPrompt(sessionPath)` — session-dependent system prompts

## Plugin Layout

The plugin directory must be named after the main assembly:

```
~/.config/bashgpt/plugins/
  MyPlugin/
    MyPlugin.dll        ← main assembly (name must match directory)
    SomeDependency.dll  ← private dependencies (optional)
```

**Rules:**
- Public, non-abstract `ITool` and `AgentBase` classes are instantiated automatically. The constructor must either be parameterless or have all parameters with default values.
- Built-in tools and agents win on name/ID collision — duplicates are logged to stderr and skipped.
- Each plugin runs in its own `AssemblyLoadContext`. SDK contracts (`bashGPT.Tools`, `bashGPT.Agents`) fall back to the host version to preserve type identity.

## Installation

```bash
mkdir -p ~/.config/bashgpt/plugins/MyPlugin
cp MyPlugin/bin/Release/net9.0/MyPlugin.dll ~/.config/bashgpt/plugins/MyPlugin/
# copy optional dependencies as well
```

The plugin is loaded automatically the next time `bashgpt-server` or `bashgpt` (CLI) starts.

## Versioning

SDK packages follow SemVer 2:

| Change | Version |
|---|---|
| Breaking change in `ITool`, `AgentBase`, `AgentLlmConfig` | Major bump |
| New non-breaking API | Minor bump |
| Bug fixes | Patch bump |

> **Note:** While the version is `0.x`, minor bumps may contain breaking changes. Pin plugin projects to a compatible version range.

## Security Note

Plugins run fully trusted in the same process with no sandboxing. Only install plugins from sources you control.
