# Repository Guidelines

## Project Structure & Module Organization
- `src/bashGPT/` contains the CLI app and core logic.
- `src/bashGPT/Cli/` handles command-line parsing and prompt handling.
- `src/bashGPT/Configuration/` manages config loading, saving, and env overrides.
- `src/bashGPT/Providers/` contains LLM provider integrations and abstractions.
- `src/bashGPT/Shell/` includes shell context collection and command execution.
- `tests/bashGPT.Tests/` holds xUnit tests mirroring the main namespaces.

## Build, Test, and Development Commands
- `dotnet build` builds the solution.
- `dotnet run --project src/bashGPT -- "<prompt>"` runs the CLI with a prompt.
- `dotnet test` runs all tests.
- `dotnet test --collect:"XPlat Code Coverage"` generates coverage via coverlet.

## Coding Style & Naming Conventions
- C# with nullable reference types enabled and implicit usings on.
- Indentation: 4 spaces. Use file-scoped namespaces.
- Naming: `PascalCase` for types and public members, `camelCase` for locals/parameters.
- Interfaces use the `I` prefix (e.g., `ILlmProvider`).
- Keep classes small and focused by area (`Cli`, `Providers`, `Shell`).

## Testing Guidelines
- Framework: xUnit with `Fact` attributes.
- Test naming follows `Method_Condition_Result` (e.g., `StreamAsync_StopsAtDone`).
- Put new tests under `tests/bashGPT.Tests/` matching the production namespace.

## Commit & Pull Request Guidelines
- Commit messages follow a conventional pattern like `feat: <summary>` (often German). Use a short, descriptive summary.
- PRs should include:
  - A brief description of the change.
  - Test results (command run and outcome).
  - Notes on config or environment changes if applicable.

## Configuration & Environment Tips
- Default config is stored at `~/.config/bashgpt/config.json`.
- Environment overrides include `BASHGPT_PROVIDER`, `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`, `BASHGPT_OLLAMA_URL`, and `BASHGPT_OLLAMA_MODEL`.
- Prefer updating config via the CLI: `dotnet run --project src/bashGPT -- config set <key> <value>`.
