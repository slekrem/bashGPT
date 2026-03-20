# Plugin Development Guide

This guide covers everything you need to build and deploy an external plugin for bashGPT.

## Overview

bashGPT discovers external tools and agents at startup by scanning a plugin directory.
No code changes to the host application are required — drop a DLL into the right folder
and restart the server or CLI.

```
~/.config/bashgpt/plugins/
  MyPlugin/
    MyPlugin.dll       ← main plugin assembly (name must match the subdirectory)
    SomeDep.dll        ← any plugin-private dependency (optional)
```

Both the embedded server (`bashgpt-server`) and the CLI (`bashgpt`) scan this directory.

---

## Quickstart

### 1. Create a class library

```bash
dotnet new classlib -n MyPlugin -f net9.0
cd MyPlugin
```

### 2. Reference the public SDK

Edit `MyPlugin.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="bashGPT.Tools" Version="*" />
  <PackageReference Include="bashGPT.Agents" Version="*" />
</ItemGroup>
```

> **Important:** Use `<SelfContained>false</SelfContained>` (the default for class libraries).
> Self-contained publishing bundles its own copies of `bashGPT.Tools` and `bashGPT.Agents`,
> which breaks the type-identity check that `PluginLoader` uses to find `ITool` and `AgentBase`.

### 3. Implement a tool

```csharp
using System.Text.Json;
using bashGPT.Tools.Abstractions;

public sealed class GreetTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "greet",
        Description: "Returns a greeting for the given name.",
        Parameters:
        [
            new ToolParameter("name", "string", "Name to greet.", Required: true),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(call.ArgumentsJson);
        var name = args.GetProperty("name").GetString() ?? "world";
        return Task.FromResult(new ToolResult(true, $"Hello, {name}!"));
    }
}
```

### 4. Implement an agent (optional)

```csharp
using bashGPT.Agents;

public sealed class GreetAgent : AgentBase
{
    public override string Id   => "greet";
    public override string Name => "Greet Agent";

    public override IReadOnlyList<string> EnabledTools => ["greet"];

    public override IReadOnlyList<string> SystemPrompt =>
        ["You are a friendly agent. Always greet the user by name using the greet tool."];

    protected override string GetAgentMarkdown() => """
        # Greet Agent

        Uses the `greet` tool to produce a personalised greeting.
        """;
}
```

### 5. Build

```bash
dotnet build -c Release
```

### 6. Deploy

```bash
# Create the plugin subdirectory (name must match the DLL)
mkdir -p ~/.config/bashgpt/plugins/MyPlugin

# Copy the output
cp bin/Release/net9.0/*.dll ~/.config/bashgpt/plugins/MyPlugin/

# Optional: copy the .deps.json for accurate dependency resolution
cp bin/Release/net9.0/MyPlugin.deps.json ~/.config/bashgpt/plugins/MyPlugin/
```

### 7. Verify

Start the server and check `/api/tools` and `/api/agents`:

```bash
dotnet run --project src/bashGPT.Server -- --no-browser
curl http://localhost:5050/api/tools | jq '.[] | .name'
curl http://localhost:5050/api/agents | jq '.[].id'
```

Any loading errors appear on stderr:

```
[plugin] MyPlugin: Failed to load assembly: ...
```

---

## Discovery rules

| Rule | Detail |
|---|---|
| **One subdirectory per plugin** | Each direct child of `~/.config/bashgpt/plugins/` is treated as one plugin. Nested directories are ignored. |
| **Naming convention** | The main DLL must match the subdirectory name (`MyPlugin/MyPlugin.dll`). Falls back to the first `*.dll` found if no match exists. |
| **Public, non-abstract classes only** | Only `public`, non-`abstract` classes are scanned. Internal and abstract types are ignored. |
| **Parameterless constructor required** | Both `ITool` and `AgentBase` implementations must have a public parameterless constructor. |
| **Built-ins win on collision** | If a plugin registers a tool name or agent ID that already exists in the built-ins, the plugin entry is skipped and a warning is written to stderr. |
| **Errors are non-fatal** | A broken plugin does not abort startup. Errors are collected in `PluginLoadResult.Errors` and written to stderr. |

---

## Isolation model

Each plugin subdirectory gets its own `AssemblyLoadContext`, backed by
`AssemblyDependencyResolver`. This means:

- **Plugin dependencies stay private.** Two plugins can each ship different versions of
  the same library without conflict.
- **Shared contracts resolve from the host.** `bashGPT.Tools` and `bashGPT.Agents`
  are already loaded in the default context. The plugin load context falls back to
  those versions instead of loading its own copy, which preserves the `ITool` and
  `AgentBase` type identities that `PluginLoader` relies on.

---

## Public SDK surface

The stable extension contracts are:

| Assembly | Public types |
|---|---|
| `bashGPT.Tools` | `ITool`, `ToolDefinition`, `ToolCall`, `ToolResult`, `ToolParameter` |
| `bashGPT.Agents` | `AgentBase`, `AgentRegistry`, `AgentLlmConfig` |

Everything else — `bashGPT.Server`, `bashGPT.Cli`, `bashGPT.Core`, and all built-in
tool and agent packages — is **not** part of the plugin SDK. Do not reference these
from an external plugin.

---

## Versioning and compatibility

bashGPT follows **semantic versioning**. The plugin SDK contracts above are the
versioned surface:

| Change | Version bump |
|---|---|
| Adding a new optional member to `AgentBase` | patch or minor |
| Adding a new type to `bashGPT.Tools` or `bashGPT.Agents` | minor |
| Removing or renaming a member of `ITool` or `AgentBase` | **major** |
| Changing a method signature in the SDK | **major** |

**Plugin compatibility rule:** A plugin compiled against SDK version `X.y.z` is
compatible with any host running `X.*.*` (same major version). A major-version bump
of the SDK requires recompiling the plugin.

The target framework (`net9.0`) is part of the compatibility contract. If the host
upgrades to a newer TFM, plugins may need to be recompiled.

---

## Security and trust model

> **Plugins run as fully trusted code in the same process with no sandboxing.**

- A plugin DLL has the same permissions as the bashGPT process itself — full file
  system access, network access, and the ability to spawn child processes.
- There is intentionally no sandboxing, signature verification, or code-access policy.
  The runtime overhead and complexity would outweigh the benefit for a local, single-user
  tool.
- **Only load plugins from sources you control.** Treat a plugin DLL the same as any
  other executable you run on your machine.
- If you build plugins from third-party source, review the code before running it.

---

## Example project

A complete, runnable example is available in [`examples/MyPlugin/`](../examples/MyPlugin/).
It implements `EchoTool` (an `ITool`) and `EchoAgent` (an `AgentBase`) in the same assembly.

Build and deploy it:

```bash
cd examples/MyPlugin
dotnet build -c Release
mkdir -p ~/.config/bashgpt/plugins/MyPlugin
cp bin/Release/net9.0/*.dll ~/.config/bashgpt/plugins/MyPlugin/
cp bin/Release/net9.0/MyPlugin.deps.json ~/.config/bashgpt/plugins/MyPlugin/
```

---

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Tool/agent does not appear | DLL name does not match subdirectory name; no `public` non-`abstract` class found; class has no parameterless constructor |
| `[plugin] ... Failed to load assembly` | Missing dependency DLL; wrong target framework; corrupted DLL |
| `[plugin] ... conflicts with a built-in` | Plugin tool name or agent ID matches a built-in — rename the plugin entry |
| Type cast fails at runtime | Plugin was built as self-contained — rebuild as framework-dependent |
