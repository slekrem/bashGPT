# bashGPT.Tools

Shared tool contracts for bashGPT.

This project defines the abstraction layer used by the server runtime and by concrete tool projects such as `bashGPT.Tools.Shell`, `bashGPT.Tools.Fetch`, and `bashGPT.Tools.Git`.

## Purpose

`bashGPT.Tools` is the foundation for:

- built-in tools shipped with bashGPT
- tool registration in the server runtime
- custom tools developed by users for their own agents

## Structure

- `Abstractions/`: public tool contracts and shared models such as `ITool`, `ToolDefinition`, `ToolCall`, and `ToolResult`
- `Registration/`: registration and composition infrastructure such as `ToolRegistry`
- `Builtins/`: simple built-in tools used for defaults and tests

## Core concepts

### `ITool`

Every tool implements `ITool`:

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
}
```

### `ToolDefinition`

`ToolDefinition` describes the name, human-readable description, and parameter schema that are exposed to the LLM.

### `ToolCall`

`ToolCall` contains the runtime invocation data:

- `Name`: the tool name
- `ArgumentsJson`: raw JSON arguments provided by the model
- `SessionPath`: optional session-scoped storage path assigned by the server

### `ToolResult`

`ToolResult` returns:

- `Success`: whether the execution succeeded
- `Content`: the tool output returned to the model

## Creating a custom tool

Create a class library that references `bashGPT.Tools`, then implement `ITool`.

```csharp
using BashGPT.Tools.Abstractions;

namespace MyCompany.BashGptTools;

public sealed class HelloTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "hello",
        Description: "Returns a greeting.",
        Parameters:
        [
            new ToolParameter("name", "string", "Name to greet.", Required: false),
        ]);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(
            Success: true,
            Content: "Hello from a custom tool."));
    }
}
```

## Registering a custom tool

Register your tool in a `ToolRegistry` and provide that registry to the server runtime.

```csharp
using BashGPT.Tools.Registration;

var toolRegistry = new ToolRegistry([
    new HelloTool(),
]);
```

## Design notes

- Keep tool contracts independent from provider-specific LLM models.
- Tools should not depend on `bashGPT.Server`.
- Prefer explicit JSON argument parsing inside the tool implementation.
- Use separate tool projects for larger domains such as shell, git, filesystem, build, testing, or fetch.
