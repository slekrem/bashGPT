# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **File-based logging** — Serilog rolling file sink writes warnings and errors to
  `~/.config/bashgpt/logs/` (Windows: `%APPDATA%\bashgpt\logs\`); 14 daily files retained (#100)

## [0.1.0] - 2026-03-24

### Added

- **Plugin system** — external tools and agents are discovered at startup from
  `~/.config/bashgpt/plugins/` using isolated `AssemblyLoadContext` per plugin (#209)
- **ShellAgent** — self-contained agent with automatic shell detection (`bash`, `zsh`,
  `cmd`, `pwsh`) and owned tool execution; shipped as a bundled plugin (#215)
- **Plugin SDK** — `bashGPT.Tools` and `bashGPT.Agents` published as NuGet packages
  so third-party plugin authors can build against a stable contract (#217)
- **ASP.NET Core Web API** — server host migrated to a full Web API with controllers,
  DI extensions, and OpenAPI spec at `/openapi/v1.json` (Development only) (#221)
- **SSE streaming** — `POST /api/chat/stream` endpoint with Server-Sent Events for
  real-time token streaming in the browser UI (#95)
- **Tool-calling** — LLM-driven tool-call loop with support for multiple rounds;
  initial tools: `bash_exec`, `filesystem_read/write/search`, `git_*`, `fetch`,
  `build_run`, `test_run` (#12, #108)
- **Agent framework** — `AgentBase` with owned tools, system prompts, `AgentRegistry`,
  and an info panel rendered in the browser UI (#208, #204)
- **DevAgent** — context-aware development agent with `context_*` tools and a file
  cache; shipped as a bundled plugin (#213)
- **Chat info panel** — per-agent markdown info panel in the browser UI (#44)
- **Sessions** — persistent chat sessions stored under `~/.config/bashgpt/sessions/`
  with metadata index and per-session message history (#221)
- **Settings UI** — browser-based settings panel for Ollama provider, model, and base
  URL with a live connection test (#169)
- **Version endpoint** — `GET /api/version` returns build metadata (#172)
- **Coverage report** — `./scripts/coverage-report.sh` generates an HTML coverage
  report with CRAP scores (#89)
- **Community health files** — `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`,
  issue templates, and PR template (#167, #168)
- **Dependabot** — automated weekly dependency updates for NuGet and npm (#179)
- **Dependency review** — GitHub Actions workflow blocks PRs that introduce
  dependencies with known vulnerabilities (severity ≥ moderate) (#179)

### Changed

- **Project structure** — reorganized into numbered layers
  (`01_core` → `02_abstractions` → `03_tools` → `04_agents` → `05_plugins` → `06_app`)
  for clearer dependency direction (#211)
- **Server architecture** — `bashGPT.Server` is now a standard ASP.NET Core Web API;
  frontend is built by MSBuild and served as static files from `wwwroot/` (#221)
- **Tool abstraction** — `ITool` / `ToolRegistry` strengthened; `ToolCall` and
  `ToolResult` are now the canonical execution contract (#204)
- **Agent abstraction** — `AgentBase` drives owned tools, `EnabledTools`, system
  prompts, and tool routing; agents no longer depend on the registry directly (#208)
- **Chat orchestration** — shared `ChatOrchestrator` extracted from CLI and server;
  both hosts use the same tool-call loop (#202)
- **Frontend language** — all UI labels, comments, and error messages translated to
  English (#223)
- **Settings contract** — dead server-side settings removed; Cerebras provider
  support dropped; only Ollama is supported (#164, #178, #219)
- **Toolchain pinning** — `.NET 9.0.301` and `Node.js ≥ 20.19.0` pinned in
  `global.json` and CI for reproducible builds (#171, #173)
- **System.CommandLine** — upgraded from preview to stable `2.0.2` (#173)

### Fixed

- DOMPurify updated to patched version to resolve a sanitization bypass (#194)
- `npm install` now runs automatically on a fresh checkout (#74)
- `dotnet run` works correctly on machines without the exact .NET 9 runtime installed
  via `rollForward` policy (#76)
- Agent chat polling and scroll behaviour stabilized in the browser UI (#106)
- Server error messages are sanitized before being returned to the client (#177)

### Security

- Server tool defaults hardened; the browser UI only exposes a safe subset of tools
  for manual selection (#166)
- Server error responses no longer leak internal exception details (#177)
- DOMPurify updated to address a sanitization bypass vulnerability (#194)

[Unreleased]: https://github.com/slekrem/bashGPT/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/slekrem/bashGPT/releases/tag/v0.1.0
