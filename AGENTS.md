# Repository Guidelines

## Project Structure & Module Organization

- `src/bashGPT.Core/` contains shared domain logic (configuration, providers, shell, CLI runners, session storage).
- `src/bashGPT.Cli/` contains the CLI executable and command-line parsing.
- `src/bashGPT.Server/` contains the server executable and HTTP/UI host.
- `src/bashGPT.Agents/` contains the `AgentBase` abstraction, `AgentRegistry`, and `GenericAgent`.
- `src/bashGPT.Agents.Shell/` contains the Shell agent (`shell`).
- `src/bashGPT.Agents.Dev/` contains the Dev agent (`dev`) with filesystem, git, build, and test tools.
- `src/bashGPT.Tools/` contains the tool abstraction (`ITool`, `ToolRegistry`, `ToolDefinition`, `ToolCall`, `ToolResult`).
- `src/bashGPT.Tools.Shell/` contains the `shell_exec` tool.
- `src/bashGPT.Tools.Filesystem/` contains `filesystem_read`, `filesystem_write`, `filesystem_search`.
- `src/bashGPT.Tools.Git/` contains `git_status`, `git_diff`, `git_log`, `git_branch`, `git_add`, `git_commit`, `git_checkout`.
- `src/bashGPT.Tools.Build/` contains the `build_run` tool.
- `src/bashGPT.Tools.Testing/` contains the `test_run` tool.
- `src/bashGPT.Tools.Fetch/` contains the `fetch` tool (HTTP GET with HTML extraction).
- `tests/bashGPT.Core.Tests/`, `tests/bashGPT.Cli.Tests/`, `tests/bashGPT.Server.Tests/` — Kernprojekte
- `tests/bashGPT.Agents.Tests/`, `tests/bashGPT.Agents.Shell.Tests/`, `tests/bashGPT.Agents.Dev.Tests/` — Agenten-Tests
- `tests/bashGPT.Tools.Tests/`, `tests/bashGPT.Tools.Shell.Tests/`, `tests/bashGPT.Tools.Filesystem.Tests/`, `tests/bashGPT.Tools.Git.Tests/`, `tests/bashGPT.Tools.Build.Tests/`, `tests/bashGPT.Tools.Testing.Tests/`, `tests/bashGPT.Tools.Fetch.Tests/` — Tool-Tests
- `tests/bashGPT.Tests/` — Platzhalter für künftige Integrations-/End-to-End-Tests (noch keine Tests implementiert)

## Build, Test, and Development Commands

- `dotnet build` builds the solution (also builds the frontend bundle).
- `dotnet run --project src/bashGPT.Cli -- "<prompt>"` runs the CLI with a prompt.
- `dotnet run --project src/bashGPT.Server` starts the local server UI.
- `dotnet test` runs all tests.
- `dotnet test --collect:"XPlat Code Coverage"` generates coverage via coverlet.
- `./scripts/coverage-report.sh` regenerates coverage from scratch and creates an HTML report.

## Coding Style & Naming Conventions

- C# with nullable reference types enabled and implicit usings on.
- Indentation: 4 spaces. Use file-scoped namespaces.
- Naming: `PascalCase` for types and public members, `camelCase` for locals/parameters.
- Interfaces use the `I` prefix (e.g., `ILlmProvider`, `ITool`).
- Keep classes small and focused by area (`Cli`, `Providers`, `Shell`, `Agents`, `Tools`).

## Testing Guidelines

- Framework: xUnit with `Fact` attributes.
- Test naming follows `Method_Condition_Result` (e.g., `StreamAsync_StopsAtDone`).
- Put new tests under the matching test project and production namespace:
  - Core/Cli/Server code → `tests/bashGPT.Core.Tests/`, `tests/bashGPT.Cli.Tests/`, `tests/bashGPT.Server.Tests/`
  - Agent code → `tests/bashGPT.Agents.Tests/`, `tests/bashGPT.Agents.Shell.Tests/`, `tests/bashGPT.Agents.Dev.Tests/`
  - Tool code → `tests/bashGPT.Tools.Tests/` and the corresponding `tests/bashGPT.Tools.<Name>.Tests/` project

## Commit & Pull Request Guidelines

- Commit messages follow a conventional pattern like `feat: <summary>` (often German). Use a short, descriptive summary.
- PRs should include:
  - A brief description of the change.
  - Test results (command run and outcome).
  - Notes on config or environment changes if applicable.

## Configuration & Environment Tips

- Default config is stored at `~/.config/bashgpt/config.json`.
- Sessions are stored under `~/.config/bashgpt/sessions/` (max. 20 sessions, two-layer layout: `index.json` + per-session `<id>/content.json`).
- Environment overrides include `BASHGPT_PROVIDER`, `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`, `BASHGPT_OLLAMA_URL`, and `BASHGPT_OLLAMA_MODEL`.
- Prefer updating config via the CLI: `dotnet run --project src/bashGPT.Cli -- config set <key> <value>`.
- A legacy `~/.config/bashgpt/agents.json` file may exist from older versions — it is no longer read or written by the current code-first agent system and can be safely ignored or deleted.
- **Tool resolution priority** when a chat request arrives: `agent.EnabledTools` → `session.EnabledTools` → `body.enabledTools` (first non-empty list wins). Resolved via `ToolHelper.Resolve()`.
- The server backend does **not** evaluate `execMode` from chat requests — tool behaviour is fixed per tool implementation (no interactive terminal in browser context).
- Loop detection (`loopDetectionEnabled`, default `true`) aborts the tool-call loop if the LLM issues identical tool calls twice in a row. Max rounds: `maxToolCallRounds` (default `8`).

## Implementing a New Agent

Agents are defined in code by subclassing `AgentBase` (in `src/bashGPT.Agents/`). No JSON configuration is needed — the class definition drives both the LLM system prompt and the Web UI info panel.

```csharp
public sealed class MyAgent : AgentBase
{
    public override string Id   => "my-agent";
    public override string Name => "My Agent";

    // List of tool IDs this agent is allowed to use
    public override IReadOnlyList<string> EnabledTools =>
    [
        "shell_exec",
        "filesystem_read",
    ];

    // One or more system-prompt strings sent to the LLM per request
    public override IReadOnlyList<string> SystemPrompt =>
    [
        "Du bist ein spezialisierter Assistent für ...",
    ];

    // Optional: LLM-Parameter überschreiben (alle Felder optional)
    // Vollständige AgentLlmConfig-Felder:
    //   Model, Temperature, TopP, NumCtx, MaxTokens, Seed,
    //   ReasoningEffort, ParallelToolCalls, Stream,
    //   FrequencyPenalty, PresencePenalty, Stop, ResponseFormat
    public override AgentLlmConfig LlmConfig => new(
        Temperature: 0.2,
        NumCtx:      32768,
        Stream:      true
    );

    // Markdown shown in the Web UI info panel (LLM config is appended automatically)
    protected override string GetAgentMarkdown() => """
        # My Agent

        Beschreibung des Agenten.
        """;
}
```

Register the agent in the server startup (e.g., `src/bashGPT.Server/Program.cs`) by adding it to the `AgentRegistry`:

```csharp
var agentRegistry = new AgentRegistry([new ShellAgent(), new DevAgent(), new MyAgent()]);
```

## Implementing a New Tool

Tools implement `ITool` (in `src/bashGPT.Tools/Abstractions/ITool.cs`):

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
}
```

- `ToolDefinition` declares the tool name, description, and JSON-Schema parameters shown to the LLM.
- `ToolResult` carries the string output returned to the LLM as a tool-result message.
- `ToolCall.SessionPath` is the path to the current session directory (set automatically by the server); tools that need session-scoped persistence (like context_* tools) use it.
- Valid `ToolParameter.Type` values: `"string"`, `"integer"`, `"object"`, `"array"` — passed directly to the LLM as JSON Schema.
- Note: `ToolCall` in `BashGPT.Tools.Abstractions` (`Name`, `ArgumentsJson`, `SessionPath`) differs from `BashGPT.Providers.ToolCall` (`Id`, `Name`, `ArgumentsJson`, `Index`) — the server converts between them.
- Register the tool in the `ToolRegistry` during server startup (`src/bashGPT.Server/Program.cs`).
- Add the tool's name to `EnabledTools` in any agent that should be able to use it.
