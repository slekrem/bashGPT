# CLAUDE.md

This file provides guidance to Claude Code (`claude.ai/code`) when working with code in this repository.

## Commands

```bash
# Build (frontend bundle is built automatically by the server project)
dotnet build
dotnet build --configuration Release

# Tests
dotnet test
dotnet test --configuration Release
dotnet test --filter "DisplayName~StreamAsync_StopsAtDone"
dotnet test --filter "FullyQualifiedName~DevAgent"
dotnet test --filter "FullyQualifiedName~ShellAgent"
dotnet test tests/03_tools/bashGPT.Tools.Git.Tests/bashGPT.Tools.Git.Tests.csproj

# Run CLI
dotnet run --project src/06_app/bashGPT.Cli -- "zeige alle .cs Dateien"

# Run server UI
dotnet run --project src/06_app/bashGPT.Server
dotnet run --project src/06_app/bashGPT.Server -- --port 6060 --no-browser

# Frontend only
cd src/06_app/bashGPT.Web && npm test
cd src/06_app/bashGPT.Web && npm run build
cd src/06_app/bashGPT.Web && npm run dev

# Coverage report
./scripts/coverage-report.sh
```

`src/06_app/bashGPT.Server/bashGPT.Server.csproj` embeds `src/06_app/bashGPT.Web/dist/index.html` and `src/06_app/bashGPT.Web/dist/bundle.js` as resources. `dotnet build` restores frontend dependencies via `npm ci` and builds the frontend automatically. Required toolchain: `.NET 9 SDK` and `Node.js >= 20.19.0 || >= 22.12.0`.

## Architecture

bashGPT is a local Ollama-based shell assistant with two hosts:
- CLI for terminal-first use
- embedded HTTP server with a browser UI, sessions, agents, and tool-calling

### Project structure

| Project | Path | Purpose |
|---|---|---|
| `bashGPT.Core` | `src/01_core/bashGPT.Core` | shared configuration, providers, shell/context, CLI/server runners, storage |
| `bashGPT.Agents` | `src/02_abstractions/bashGPT.Agents` | `AgentBase`, `AgentRegistry`, shared agent config types — public plugin SDK |
| `bashGPT.Tools` | `src/02_abstractions/bashGPT.Tools` | tool abstractions and `ToolRegistry` |
| `bashGPT.Tools.Build` | `src/03_tools/bashGPT.Tools.Build` | `build_run` |
| `bashGPT.Tools.Fetch` | `src/03_tools/bashGPT.Tools.Fetch` | `fetch` |
| `bashGPT.Tools.Filesystem` | `src/03_tools/bashGPT.Tools.Filesystem` | `filesystem_read`, `filesystem_write`, `filesystem_search` |
| `bashGPT.Tools.Git` | `src/03_tools/bashGPT.Tools.Git` | `git_status`, `git_diff`, `git_log`, `git_branch`, `git_add`, `git_commit`, `git_checkout` |
| `bashGPT.Tools.Shell` | `src/03_tools/bashGPT.Tools.Shell` | `shell_exec` |
| `bashGPT.Tools.Testing` | `src/03_tools/bashGPT.Tools.Testing` | `test_run` |
| `bashGPT.Agents.Dev` | `src/04_agents/bashGPT.Agents.Dev` | Dev agent plus `context_*` tools and `ContextFileCache` |
| `bashGPT.Agents.Shell` | `src/04_agents/bashGPT.Agents.Shell` | Shell agent with `shell_exec` |
| `bashGPT.Plugins` | `src/05_plugins/bashGPT.Plugins` | `PluginLoader`, `PluginLoadContext` — discovers external tools and agents from `~/.config/bashgpt/plugins/` |
| `bashGPT.Plugins.TestFixtures` | `src/05_plugins/bashGPT.Plugins.TestFixtures` | `FakeToolFixture`, `FakeAgentFixture` — test helpers for plugin loader tests (not packaged) |
| `bashGPT.Cli` | `src/06_app/bashGPT.Cli` | CLI host based on `System.CommandLine` |
| `bashGPT.Server` | `src/06_app/bashGPT.Server` | embedded HTTP/UI host and API handlers |
| `bashGPT.Web` | `src/06_app/bashGPT.Web` | Lit/TypeScript frontend source |

Current test projects:
- `tests/01_core/bashGPT.Core.Tests`
- `tests/02_abstractions/bashGPT.Agents.Tests`
- `tests/02_abstractions/bashGPT.Tools.Tests`
- `tests/03_tools/bashGPT.Tools.Build.Tests`
- `tests/03_tools/bashGPT.Tools.Fetch.Tests`
- `tests/03_tools/bashGPT.Tools.Filesystem.Tests`
- `tests/03_tools/bashGPT.Tools.Git.Tests`
- `tests/03_tools/bashGPT.Tools.Shell.Tests`
- `tests/03_tools/bashGPT.Tools.Testing.Tests`
- `tests/05_plugins/bashGPT.Plugins.Tests`
- `tests/06_app/bashGPT.Cli.Tests`
- `tests/06_app/bashGPT.Server.Tests`

### Main execution flows

**CLI (`CliChatRunner`)**
```text
prompt
  -> ShellContextCollector
  -> provider request (Ollama)
  -> tool-call loop via ChatOrchestrator / CLI runner
  -> CommandExecutor for extracted shell commands
  -> console output
```

The CLI still supports execution modes (`ask`, `dry-run`, `auto-exec`, `no-exec`) and optional `forceTools`.

**Server (`ServerChatRunner`)**
```text
POST /api/chat or /api/chat/stream
  -> SessionStore load/create
  -> AgentRegistry lookup
  -> tool resolution via ToolHelper.Resolve()
  -> provider request (Ollama)
  -> tool-call loop
  -> SessionStore persist
  -> JSON response or SSE stream
```

Server-side settings are intentionally simpler than before:
- only Ollama is supported
- settings API persists only provider/model/base URL for the Ollama-backed server UI
- command timeout, loop detection and max tool-call rounds now come from `AppDefaults`
- there is no configurable rate limiter anymore

### Important classes

| Class | Path | Purpose |
|---|---|---|
| `CliChatRunner` | `src/01_core/bashGPT.Core/Cli/CliChatRunner.cs` | CLI prompt flow and command execution |
| `ServerChatRunner` | `src/01_core/bashGPT.Core/Cli/ServerChatRunner.cs` | server chat loop with sessions, agents, tools |
| `ChatOrchestrator` | `src/01_core/bashGPT.Core/Cli/ChatOrchestrator.cs` | shared request orchestration helpers |
| `ConfigurationService` | `src/01_core/bashGPT.Core/Configuration/ConfigurationService.cs` | `config.json` load/save and env overrides |
| `ProviderFactory` | `src/01_core/bashGPT.Core/Providers/ProviderFactory.cs` | creates the active Ollama provider |
| `OllamaProvider` | `src/01_core/bashGPT.Core/Providers/OllamaProvider.cs` | OpenAI-compatible Ollama integration |
| `ShellContextCollector` | `src/01_core/bashGPT.Core/Shell/ShellContextCollector.cs` | shell/git/system context for prompts |
| `CommandExecutor` | `src/01_core/bashGPT.Core/Shell/CommandExecutor.cs` | shell command execution with guardrails |
| `SessionStore` | `src/01_core/bashGPT.Core/Storage/SessionStore.cs` | thread-safe session persistence |
| `ServerHost` | `src/06_app/bashGPT.Server/Server/ServerHost.cs` | HTTP listener and routing |
| `AgentBase` | `src/02_abstractions/bashGPT.Agents/AgentBase.cs` | code-first base type for agents |
| `AgentRegistry` | `src/02_abstractions/bashGPT.Agents/AgentRegistry.cs` | in-memory agent lookup |
| `ContextFileCache` | `src/04_agents/bashGPT.Agents.Dev/ContextFileCache.cs` | loaded-file cache for dev agent |
| `ToolRegistry` | `src/02_abstractions/bashGPT.Tools/Registration/ToolRegistry.cs` | available `ITool` implementations |
| `RunningChatRegistry` | `src/06_app/bashGPT.Server/Server/RunningChatRegistry.cs` | request cancellation support |
| `LegacyHistory` | `src/06_app/bashGPT.Server/Server/LegacyHistory.cs` | compatibility layer for `/api/history` and `/api/reset` |

### Agents

Agents are defined in code, not JSON. The server registers:
- `generic`
- `dev`
- `shell`

`AgentBase` drives:
- stable agent id
- display name
- enabled tool set
- one or more system prompt messages
- optional `AgentLlmConfig`
- markdown info panel for the UI

`GetInfoPanelMarkdown()` automatically appends the effective LLM config.

### Tools

All tools implement:

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
}
```

Important details:
- `BashGPT.Tools.Abstractions.ToolCall` is the tool execution contract (`Name`, `ArgumentsJson`, `SessionPath`)
- `BashGPT.Providers.ToolCall` is the LLM/provider-side tool-call model
- server code converts between the two
- the browser UI only exposes a safe default subset of tools for manual selection; agents can still have broader fixed tool sets

### HTTP API

Current endpoints:

```text
GET  /                        -> embedded index.html
GET  /bundle.js               -> embedded frontend bundle
GET  /api/context
GET  /api/settings
PUT  /api/settings
POST /api/settings/test
POST /api/chat
POST /api/chat/stream
POST /api/chat/cancel
GET  /api/sessions
POST /api/sessions
POST /api/sessions/clear
GET  /api/sessions/<id>
PUT  /api/sessions/<id>
DELETE /api/sessions/<id>
GET  /api/tools
GET  /api/agents
GET  /api/agents/<id>/info-panel
GET  /api/history      # legacy compatibility
POST /api/reset        # legacy compatibility
```

Notes:
- `POST /api/chat/cancel` expects `{ "requestId": "..." }`
- `POST /api/chat/stream` is SSE-based
- tool resolution priority is `agent.EnabledTools` -> `session.EnabledTools` -> request `enabledTools`

### Session storage

Sessions live under `~/.config/bashgpt/sessions/`:
- `index.json` for session metadata
- `<id>/content.json` for messages and per-session state
- `<id>/requests/` for recorded request/debug artifacts

`SessionStore` writes atomically and keeps at most 20 sessions.

### Plugin discovery

External tools and agents are loaded at startup from `~/.config/bashgpt/plugins/`.

**Directory layout:**
```
~/.config/bashgpt/plugins/
  MyPlugin/
    MyPlugin.dll       ← main plugin assembly (name must match subdirectory)
    SomeDep.dll        ← plugin-private dependency (optional)
```

**Discovery rules:**
- Each subdirectory is one plugin. The main DLL must match the subdirectory name (e.g. `MyPlugin/MyPlugin.dll`). Falls back to the first `*.dll` found if the convention is not followed.
- Public, non-abstract `ITool` and `AgentBase` classes with a public parameterless constructor are instantiated automatically.
- Built-ins always win on name/ID collision — duplicates are logged to stderr and skipped.
- Per-plugin `AssemblyLoadContext` isolates dependency graphs; shared contracts (`bashGPT.Tools`, `bashGPT.Agents`) fall back to the host version to preserve type identity.
- Plugin assemblies are fully trusted and run in the same process with no sandboxing.

Both the Server (`ServerApplication.LoadPlugins`) and the CLI (`CliApplication.LoadPlugins`) perform discovery at startup. Non-fatal loading errors are written to stderr and never abort the process.

### Configuration

Config file: `~/.config/bashgpt/config.json`

Supported `config set` keys:
- `defaultProvider`
- `execMode`
- `forceTools`
- `ollama.baseUrl`
- `ollama.model`

Environment overrides:
- `BASHGPT_PROVIDER`
- `BASHGPT_OLLAMA_URL`
- `BASHGPT_OLLAMA_MODEL`
- `BASHGPT_EXEC_MODE`
- `BASHGPT_FORCE_TOOLS`
- `BASHGPT_SERVER_ALLOWED_TOOLS`

Not configurable through the server settings UI:
- `execMode`
- `forceTools`

No longer configurable as runtime server defaults:
- `commandTimeoutSeconds`
- `loopDetectionEnabled`
- `maxToolCallRounds`
- any `rateLimiting.*` settings

Those runtime defaults are now fixed internally via `AppDefaults`.

## Coding style

- C# with nullable reference types and implicit usings
- 4 spaces, file-scoped namespaces
- `PascalCase` for types/members, `camelCase` for locals
- xUnit tests using `Method_Condition_Result`
- conventional commits like `feat:`, `fix:`, `test:`, `docs:`
