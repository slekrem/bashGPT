# bashGPT

[![CI](https://github.com/slekrem/bashGPT/actions/workflows/ci.yml/badge.svg)](https://github.com/slekrem/bashGPT/actions/workflows/ci.yml)
[![NuGet bashGPT.Tools](https://img.shields.io/nuget/v/bashGPT.Tools.svg?label=bashGPT.Tools)](https://www.nuget.org/packages/bashGPT.Tools)
[![NuGet bashGPT.Agents](https://img.shields.io/nuget/v/bashGPT.Agents.svg?label=bashGPT.Agents)](https://www.nuget.org/packages/bashGPT.Agents)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Local AI assistant for shell workflows. bashGPT can optionally collect context such as the current directory, git status, OS, and shell, send it to an LLM through Ollama, and help execute the resulting shell commands with the right safety level.

## Features
- CLI built with `System.CommandLine`
- Ollama provider support
- Optional shell context (directory, OS, shell, git)
- Safety confirmation for dangerous commands
- Streaming responses
- Browser UI with chat history, session management, and agent selection
- Specialized agents (`shell`, `dev`) with dedicated tool sets
- Modular tool ecosystem for filesystem, git, build, testing, fetch, and shell workflows
- Plugin system for external tools and agents via `~/.config/bashgpt/plugins/`
- `bashGPT.Tools` and `bashGPT.Agents` available as NuGet packages for plugin development
- MIT licensed

## Installation
Recommended versions for reproducible release checks:
- .NET SDK `9.0.301`
- Node.js `20.19.0`

For the frontend build, Vite 7 also supports:
- Node.js `>= 20.19.0`
- Node.js `>= 22.12.0`

Additional local dependencies depending on usage:
- `git` for git tools and git context (`git --version`)
- `ollama` for local model access in CLI and server mode (`ollama --version`, then `ollama serve`)
- `npm` for the web build and the `build_run` / `test_run` npm runner (`npm --version`)
- `pytest` only if you want to use the `pytest` runner in `test_run` (`pytest --version`)

Check versions:
- CLI: `dotnet run --project src/06_app/bashGPT.Cli -- --version`
- Server: `GET /api/version`

```bash
# Clone the repo, then build (npm install + npm run build are triggered automatically)
dotnet build
```

## Quick Start
```bash
# Simple request
dotnet run --project src/06_app/bashGPT.Cli -- "show all .cs files"

# Choose provider
dotnet run --project src/06_app/bashGPT.Cli -- --provider ollama "list all tests"

# Override model
dotnet run --project src/06_app/bashGPT.Cli -- --model llama3.2 "show changed files"

# Explicitly enforce tool calls (optional)
dotnet run --project src/06_app/bashGPT.Cli -- --force-tools "analyze this directory"
```

## Configuration
By default, the config file is stored at `~/.config/bashgpt/config.json`.

```bash
# Show configuration
dotnet run --project src/06_app/bashGPT.Cli -- config list

# Set provider
dotnet run --project src/06_app/bashGPT.Cli -- config set defaultProvider ollama

# Set Ollama URL and model
dotnet run --project src/06_app/bashGPT.Cli -- config set ollama.baseUrl http://localhost:11434
dotnet run --project src/06_app/bashGPT.Cli -- config set ollama.model llama3.2
```

Environment variable overrides:
- `BASHGPT_PROVIDER`
- `BASHGPT_OLLAMA_URL`
- `BASHGPT_OLLAMA_MODEL`

## Execution Modes
```bash
# Show commands without executing them
dotnet run --project src/06_app/bashGPT.Cli -- --dry-run "delete all tmp files"

# Pure chat, do not execute commands
dotnet run --project src/06_app/bashGPT.Cli -- --no-exec "how do I find large files?"

# Execute without confirmation
dotnet run --project src/06_app/bashGPT.Cli -- --auto-exec "git status"
```

## Context Controls
```bash
# Do not send shell context
dotnet run --project src/06_app/bashGPT.Cli -- --no-context "how do I create a new branch?"

# Include directory contents
dotnet run --project src/06_app/bashGPT.Cli -- --include-dir "show important files"
```

## Server Mode
```bash
# Start the local server on http://127.0.0.1:5050 and open the browser
dotnet run --project src/06_app/bashGPT.Server

# Use a custom port without automatically opening the browser
dotnet run --project src/06_app/bashGPT.Server -- --port 6060 --no-browser

# Start the server with explicit model selection
dotnet run --project src/06_app/bashGPT.Server -- --provider ollama --model llama3.2 --verbose
```

Available server flags: `--provider`, `--model`, `--port`, `--no-browser`, `--verbose`.

Notes:
- The UI provides chat history, session management, agent selection, manually selectable tools, and visibility into executed tool results.
- Sessions are stored in `~/.config/bashgpt/sessions/` (max. 20 sessions).
- Available API endpoints include `/api/sessions/*`, `/api/agents`, `/api/agents/<id>/info-panel`, `/api/tools`, `/api/chat/stream`, and `/api/chat/cancel`.
- All tools discovered in the plugin directory are available for selection in the browser UI. Tool selection is UI-driven.
- Agents with fixed tool sets such as `shell` and `dev` remain an intentional trust boundary and can expose more powerful tools.

## Agents
Agents are specialized chat modes with their own system prompt and tool set. They are selected in the browser UI.

**Generic Agent** (`generic`): Default chat mode without an agent-specific system prompt. No tools are enabled by default. Tools can be selected manually in the UI.

**Shell-Agent** (`shell`): Interactive shell assistant with the `shell_exec` tool. Executes commands directly.

**Dev-Agent** (`dev`): Development agent with full access to filesystem, git, build, testing, and shell tools. It can load source files into context via `context_load_files`, `context_unload_files`, and `context_clear_files`.

## Plugin SDK

External tools and agents can be developed as standalone .NET assemblies and loaded into bashGPT without recompiling the host.

**Install via NuGet:**

```bash
# Tool SDK — implement ITool
dotnet add package bashGPT.Tools

# Agent SDK — implement AgentBase (includes bashGPT.Tools transitively)
dotnet add package bashGPT.Agents
```

| Package | Purpose |
|---|---|
| [`bashGPT.Tools`](https://www.nuget.org/packages/bashGPT.Tools) | Implement custom `ITool` types |
| [`bashGPT.Agents`](https://www.nuget.org/packages/bashGPT.Agents) | Implement custom `AgentBase` subclasses (includes `bashGPT.Tools` transitively) |

Both packages are versioned with SemVer 2. While on `0.x`, minor bumps may contain breaking changes — pin to a compatible version in plugin projects.

Plugins are placed in `~/.config/bashgpt/plugins/<PluginName>/<PluginName>.dll` and are loaded automatically at startup. Plugin assemblies run fully trusted in the same process without sandboxing — only install plugins from trusted sources.

See [docs/plugin-authoring.md](docs/plugin-authoring.md) for a full guide including code examples, layout rules, and versioning details.

## Tests
```bash
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Full HTML report (includes cleanup)
./scripts/coverage-report.sh
```

For a reproducible launch check on a clean checkout:
- `dotnet build -m:1 /nodeReuse:false`
- `dotnet test -m:1 /nodeReuse:false`
- optionally, in `src/06_app/bashGPT.Web`: `npm test`

## Open-Source Launch Checklist
Ready for a public release when:
- build and tests are green without manual follow-up
- README and public repository metadata match the current product state
- community files such as `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and `SECURITY.md` are present

Suggested first public release:
- tag `v0.1.0`
- release notes with a short overview, setup requirements, and known limitations
- deliberate review of GitHub description, topics, homepage decision, and visibility before going public
- suggested GitHub description: `Local AI assistant for shell workflows with a .NET CLI, browser UI, agents, tools, and Ollama integration.`
- suggested topics: `dotnet`, `cli`, `ollama`, `agent`, `tool-calling`
- switch repository visibility from `Private` to `Public` only as the final maintainer step after a successful launch check

## Output Format
bashGPT expects shell commands to appear in fenced code blocks. Example:

```bash
ls -la
```

## License
MIT. See [LICENSE](LICENSE).

## Community
- Contribution guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Code of conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- Security policy: [SECURITY.md](SECURITY.md)
