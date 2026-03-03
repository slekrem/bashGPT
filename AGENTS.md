# Repository Guidelines

## Project Structure & Module Organization
- `src/bashGPT.Core/` contains shared domain logic (configuration, providers, shell, storage, shared runners).
- `src/bashGPT.Cli/` contains the CLI executable and command-line parsing.
- `src/bashGPT.Server/` contains the server executable and HTTP/UI host.
- `tests/bashGPT.Core.Tests/`, `tests/bashGPT.Cli.Tests/`, and `tests/bashGPT.Server.Tests/` mirror the `src` projects with xUnit tests.

## Build, Test, and Development Commands
- `dotnet build` builds the solution.
- `dotnet run --project src/bashGPT.Cli -- "<prompt>"` runs the CLI with a prompt.
- `dotnet run --project src/bashGPT.Server` starts the local server UI.
- `dotnet test` runs all tests.
- `dotnet test --collect:"XPlat Code Coverage"` generates coverage via coverlet.
- `./scripts/coverage-report.sh` regenerates coverage from scratch and creates an HTML report.

## Coding Style & Naming Conventions
- C# with nullable reference types enabled and implicit usings on.
- Indentation: 4 spaces. Use file-scoped namespaces.
- Naming: `PascalCase` for types and public members, `camelCase` for locals/parameters.
- Interfaces use the `I` prefix (e.g., `ILlmProvider`).
- Keep classes small and focused by area (`Cli`, `Providers`, `Shell`).

## Testing Guidelines
- Framework: xUnit with `Fact` attributes.
- Test naming follows `Method_Condition_Result` (e.g., `StreamAsync_StopsAtDone`).
- Put new tests under the matching test project (`tests/bashGPT.Core.Tests/`, `tests/bashGPT.Cli.Tests/`, `tests/bashGPT.Server.Tests/`) and production namespace.

## Commit & Pull Request Guidelines
- Commit messages follow a conventional pattern like `feat: <summary>` (often German). Use a short, descriptive summary.
- PRs should include:
  - A brief description of the change.
  - Test results (command run and outcome).
  - Notes on config or environment changes if applicable.

## Configuration & Environment Tips
- Default config is stored at `~/.config/bashgpt/config.json`.
- Environment overrides include `BASHGPT_PROVIDER`, `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`, `BASHGPT_OLLAMA_URL`, and `BASHGPT_OLLAMA_MODEL`.
- Prefer updating config via the CLI: `dotnet run --project src/bashGPT.Cli -- config set <key> <value>`.
