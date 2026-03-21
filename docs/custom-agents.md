# Creating a Custom Agent

This guide explains how to extend bashGPT with a custom agent using the `bashGPT.Agents` SDK.

## Overview

Agents are plain C# classes that subclass `AgentBase`. Each agent defines:

- a stable **ID** and **display name**
- the **tools** it is allowed to call
- one or more **system prompts** sent to the LLM on every request
- an optional **LLM configuration** (temperature, context size, etc.)
- a **markdown info panel** shown in the browser UI

No JSON configuration required — just code.

## Minimal example

```csharp
using bashGPT.Agents;

public sealed class MyAgent : AgentBase
{
    public override string Id   => "my-agent";
    public override string Name => "My Agent";

    public override IReadOnlyList<string> EnabledTools =>
    [
        "shell_exec",
        "filesystem_read",
    ];

    public override IReadOnlyList<string> SystemPrompt =>
    [
        "You are a specialized assistant for infrastructure tasks.",
    ];

    protected override string GetAgentMarkdown() => """
        # My Agent

        A short description of what this agent does.
        """;
}
```

## Extension points

| Member | Required | Purpose |
|---|---|---|
| `Id` | yes | Unique, stable identifier. Never change this after deployment — sessions are keyed to it. |
| `Name` | yes | Human-readable display name shown in the UI. |
| `EnabledTools` | yes | Tool names available to this agent. Must match the names registered in `ToolRegistry`. |
| `SystemPrompt` | yes | One or more system messages sent to the LLM at the start of every request. Can be a computed property for dynamic content. |
| `LlmConfig` | no | Override temperature, top-p, context window, and other LLM parameters. Return `null` (the default) to use the server's default model settings. |
| `GetAgentMarkdown()` | yes | Markdown content for the info panel in the browser UI. The LLM configuration table is appended automatically. |

## Customizing the LLM configuration

Override `LlmConfig` to adjust model parameters:

```csharp
public override AgentLlmConfig LlmConfig => new(
    Temperature:       0.1,    // low = deterministic
    TopP:              0.95,
    NumCtx:            32768,  // context window in tokens
    MaxTokens:         4096,   // max output tokens
    ReasoningEffort:   "high",
    ParallelToolCalls: false,  // sequential is safer for file mutations
    Stream:            true
);
```

All fields are optional — specify only what you want to override.

## Dynamic system prompts

`SystemPrompt` is a regular property, so it can be computed at runtime:

```csharp
public override IReadOnlyList<string> SystemPrompt =>
[
    "You are a specialized assistant.",
    $"Current directory: {Directory.GetCurrentDirectory()}",
    $"Date: {DateTime.Now:yyyy-MM-dd}",
];
```

## Registering the agent

There are two ways to register a custom agent:

### Option A — Plugin directory (recommended for external agents)

Build your agent as a class library and drop the DLL into the plugin directory.
bashGPT discovers it automatically at startup without any code changes:

```
~/.config/bashgpt/plugins/
  MyPlugin/
    MyPlugin.dll
```

See [docs/plugins.md](plugins.md) for the full plugin development guide, including
directory layout, build instructions, versioning, and the security model.

### Option B — Code registration (for built-in or fork-based agents)

Fork the repository and add your agent to the built-in list in `ServerApplication`:

```csharp
// src/bashGPT.Server/ServerApplication.cs  (fork only — do not modify for external plugins)
private static readonly AgentBase[] _builtins =
[
    new GenericAgent(),   // internal to bashGPT.Server
    new DevAgent(),
    new ShellAgent(),
    new MyAgent(),        // ← add here
];
```

> **Note:** `GenericAgent` is `internal` to `bashGPT.Server` and is not part of the public
> agent SDK. It cannot be referenced from an external project. External consumers subclass
> `AgentBase` from `bashGPT.Agents` and deploy via the plugin directory (Option A).

## Adding custom tools

If your agent needs tools that are not yet registered, implement `ITool` and add it
to `ToolRegistry`. See [AGENTS.md](../AGENTS.md#implementing-a-new-tool) for the full
tool implementation guide.

## Boundary: SDK surface vs. built-in implementation

`bashGPT.Agents` is the public SDK layer — `AgentBase` and `AgentRegistry` are the only
types intended for external use. Built-in agents (`DevAgent`, `ShellAgent`) and the
server default (`GenericAgent`) live in their own packages and are not part of the
extension contract.

Custom agents only depend on `bashGPT.Agents` and optionally `bashGPT.Core.Models.Providers`
for `AgentLlmConfig`.
