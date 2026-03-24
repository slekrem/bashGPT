# Architecture

This document describes how the major projects in bashGPT fit together, where extension points belong, and what should and should not live in each layer. It is aimed at contributors and plugin authors.

## Layer Overview

```
06_app      bashGPT.Cli          bashGPT.Server
               │                      │
05_plugins  bashGPT.Plugins ──────────┤
               │                      │
04_agents   bashGPT.Agents.Shell  bashGPT.Agents.Dev
               │                      │
03_tools    bashGPT.Tools.*  (Build, Fetch, Filesystem, Git, Shell, Testing)
               │                      │
02_abstractions  bashGPT.Tools    bashGPT.Agents
                      └──────────────┘
                               │
01_core          bashGPT.Core
```

Dependencies point downward only. `bashGPT.Core` has no knowledge of tools, agents, or hosts. `bashGPT.Tools` and `bashGPT.Agents` have no knowledge of the CLI or server.

## Project Responsibilities

### `bashGPT.Core` (01_core)

The shared foundation. Contains everything both hosts need:

- `OllamaProvider` / `ProviderFactory` — LLM communication
- `ChatOrchestrator` — single-call request building and streaming
- `ChatSessionBootstrap` / `ChatSessionRunner` — session lifecycle and tool-call loop
- `ConfigurationService` — `config.json` load/save and environment overrides
- `AppBootstrap` — platform-aware config and plugin paths
- `CommandExecutor` — shell command execution with guardrails (CLI only)
- `SessionStore` — atomic session persistence

**What belongs here:** anything that both `bashGPT.Cli` and `bashGPT.Server` need and that does not depend on tools or agents.

**What does not belong here:** tool implementations, agent logic, HTTP controllers, CLI argument parsing.

### `bashGPT.Tools` and `bashGPT.Agents` (02_abstractions)

Public extension contracts. These two packages are published to NuGet and form the plugin SDK.

- `ITool` + `ToolDefinition` + `ToolCall` + `ToolResult` — tool contract
- `ToolRegistry` — in-memory tool lookup used by the server
- `AgentBase` — abstract base for all agents (owned tools, system prompts, LLM config, info panel)
- `AgentRegistry` — in-memory agent lookup

**What belongs here:** stable interfaces and base types that plugin authors compile against.

**What does not belong here:** concrete tool or agent implementations, host-specific code.

### `bashGPT.Tools.*` (03_tools)

Concrete tool implementations. Each project is a focused, independently testable unit:

| Project | Tools |
|---|---|
| `bashGPT.Tools.Shell` | `bash_exec`, `cmd_exec`, `pwsh_exec`, `shell_exec` |
| `bashGPT.Tools.Filesystem` | `filesystem_read`, `filesystem_write`, `filesystem_search` |
| `bashGPT.Tools.Git` | `git_status`, `git_diff`, `git_log`, `git_branch`, `git_add`, `git_commit`, `git_checkout` |
| `bashGPT.Tools.Build` | `build_run` |
| `bashGPT.Tools.Testing` | `test_run` |
| `bashGPT.Tools.Fetch` | `fetch` |

All tools implement `ITool` and have no dependency on `bashGPT.Core`.

### `bashGPT.Agents.Dev` and `bashGPT.Agents.Shell` (04_agents)

Built-in agents shipped as bundled plugins. They are not compiled into the hosts — they are copied to `~/.config/bashgpt/plugins/` during build and discovered at startup like any other plugin.

- **`DevAgent`** — hybrid agent: owns three private `context_*` tools, enables thirteen registry tools (`filesystem_*`, `git_*`, `build_run`, `test_run`, `shell_exec`, `fetch`). Builds dynamic system prompts from git context and loaded file cache.
- **`ShellAgent`** — self-contained agent: detects the active shell at construction time (bash, PowerShell, cmd.exe) and owns exactly one shell-execution tool. No registry dependency.

### `bashGPT.Plugins` (05_plugins)

Plugin discovery and isolation. `PluginLoader.LoadFromDirectory()` scans subdirectories of the plugin path, loads each DLL into its own `PluginLoadContext`, and instantiates all public non-abstract `ITool` and `AgentBase` types with a parameterless constructor. SDK contracts fall back to the host version to preserve type identity.

**Plugin directory layout:**
```
~/.config/bashgpt/plugins/   (macOS/Linux)
%APPDATA%\bashgpt\plugins\   (Windows)
  MyPlugin/
    MyPlugin.dll       ← main assembly (name must match directory)
    SomeDep.dll        ← optional private dependency
```

### `bashGPT.Cli` (06_app)

Terminal host. Uses `System.CommandLine` for argument parsing. Startup sequence:

1. `CliApplication.CreateConfigurationService()`
2. `CliApplication.LoadPlugins()` → `PluginLoader`
3. `CliApplication.CreateChatRunner(configService, pluginTools)`
4. `RootCommand` with `prompt`, `--model`, `--verbose`, `--force-tools` and `config` subcommands

The CLI has no `AgentRegistry` and no `ToolRegistry`. Plugin tools are stored in a flat dictionary and executed directly by `CliChatRunner`.

### `bashGPT.Server` (06_app)

HTTP host. ASP.NET Core Web API with a Lit/TypeScript SPA served from `wwwroot/`. Startup sequence:

1. `ServerApplication.CreateConfigurationService()`
2. `ServerApplication.LoadPlugins()` → `PluginLoader`
3. `ServerApplication.CreateToolRegistry(pluginTools)` — registers all tools
4. `ServerApplication.CreateAgentRegistry(pluginAgents)` — registers `GenericAgent` + plugins
5. `builder.Services.AddBashGptServer(...)` — DI registration
6. `app.UseBashGptPipeline()` — middleware and route mapping

## Execution Flows

### CLI

```
bashgpt "prompt"
  → CliChatRunner.RunAsync()
      → ChatSessionBootstrap.CreateAsync()       build provider + session
      → ChatSessionRunner.RunAsync()
          → ChatOrchestrator.ChatOnceAsync()     LLM call, stream tokens
          → if tool calls:
              plugin tools → ITool.ExecuteAsync()
              bash tool    → CommandExecutor      with safety confirmation
          → if commands executed: second LLM call (follow-up)
```

### Server

```
POST /api/chat  or  POST /api/chat/stream
  → ChatController
  → ServerSessionService          load or create session
  → ServerChatRunner.RunServerChatAsync()
      → ChatSessionBootstrap.CreateAsync()       build provider + session
      → ChatSessionRunner.RunAsync()
          → ChatOrchestrator.ChatOnceAsync()     LLM call, stream tokens (SSE)
          → if tool calls:
              agent.TryHandleToolCallAsync()     owned tools first
              ToolRegistry.TryGet()              fallback to registry
          → RefreshSystemMessages                agents can update prompts each round
      → SessionStore.PersistAsync()
  → JSON response or SSE stream
```

## Tool and Agent Registration

### Server startup

```
LoadPlugins()
  └─ PluginLoader discovers ITool and AgentBase types from plugin DLLs

CreateToolRegistry(pluginTools)
  └─ registers all plugin tools
     (built-in tools like git_*, filesystem_* are registered here too
      when the server project references the tool assemblies directly)

CreateAgentRegistry(pluginAgents)
  └─ starts with GenericAgent (built-in)
  └─ adds plugin agents; built-ins win on ID collision
```

### Tool resolution during a chat request

```
agent.EnabledTools        which tool names this agent exposes
  │
  └─ agent.TryHandleToolCallAsync()   checks owned tools first
       └─ if null: ToolRegistry.TryGet()   falls back to registry
```

`EnabledTools` is derived automatically from `GetOwnedTools()`. Override it to add registry tool names:

```csharp
public override IReadOnlyList<string> EnabledTools =>
[
    ..base.EnabledTools,   // owned tools
    "filesystem_read",     // from registry
    "git_status",          // from registry
];
```

## Extension Patterns

There are three patterns for building agents, ordered by increasing registry dependency:

**Self-contained** (like `ShellAgent`): implement `GetOwnedTools()`, do not override `EnabledTools`. The agent is fully independent of what is installed in the registry.

```csharp
public override IReadOnlyList<ITool> GetOwnedTools() => [new MyTool()];
```

**Hybrid** (like `DevAgent`): implement `GetOwnedTools()` for private tools and override `EnabledTools` to add registry tool names.

```csharp
public override IReadOnlyList<ITool> GetOwnedTools() => [new PrivateTool()];
public override IReadOnlyList<string> EnabledTools => [..base.EnabledTools, "git_status", "fetch"];
```

**Generic** (like `GenericAgent`): no owned tools, tool set driven entirely by user selection in the UI. `GetOwnedTools()` returns an empty list.

## What Belongs Where: Quick Reference

| Concern | Layer |
|---|---|
| LLM provider, HTTP client, request building | `bashGPT.Core` |
| Config file, environment overrides | `bashGPT.Core` |
| Session persistence | `bashGPT.Core` |
| Tool and agent contracts (`ITool`, `AgentBase`) | `bashGPT.Tools` / `bashGPT.Agents` |
| Concrete tool implementation | `bashGPT.Tools.*` or a plugin |
| Concrete agent implementation | `bashGPT.Agents.*` or a plugin |
| Plugin loading and isolation | `bashGPT.Plugins` |
| CLI argument parsing, command execution | `bashGPT.Cli` |
| HTTP controllers, sessions API, SSE streaming | `bashGPT.Server` |
| Browser UI (TypeScript/Lit) | `bashGPT.Web` |

## Current Limits

- **No dynamic registration**: tools and agents are registered once at startup and cannot be added or removed at runtime.
- **No sandboxing**: plugin assemblies run fully trusted in the same process.
- **Ollama only**: the server host supports only the Ollama provider. The CLI shares the same restriction since `ProviderFactory` in Core only constructs an `OllamaProvider`.
- **Parameterless constructor required**: plugin types must have a public parameterless constructor (or one where all parameters have defaults) to be instantiated by `PluginLoader`.
