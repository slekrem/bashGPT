# Contributing

## Scope
Contributions are welcome for bug fixes, documentation, tests, tooling, and targeted improvements to the CLI, server UI, agents, and tool system.

## Local Setup
Prerequisites:
- .NET 9 SDK
- Node.js >= 20.19.0 or >= 22.12.0
- Ollama locally if you want to exercise runtime chat flows beyond build/test

Clone the repository and run:

```bash
dotnet build -m:1 /nodeReuse:false
dotnet test -m:1 /nodeReuse:false
```

Optional web-only validation:

```bash
cd src/bashGPT.Web
npm test
```

## Project Layout
- `src/bashGPT.Core/`: shared domain logic, configuration, providers, shell, storage
- `src/bashGPT.Cli/`: CLI entrypoint and command parsing
- `src/bashGPT.Server/`: local server and embedded browser UI host
- `tests/`: mirrored test projects by area

## Coding Guidelines
- C# uses nullable reference types and implicit usings.
- Use 4 spaces and file-scoped namespaces where appropriate.
- `PascalCase` for public types and members, `camelCase` for locals and parameters.
- Keep classes focused by area and avoid broad cross-cutting changes unless required.
- Prefer ASCII unless a file already clearly uses Unicode.

## Testing Expectations
- Add or update tests with behavior changes.
- Prefer the matching test project for the changed area.
- Keep test names descriptive in the `Method_Condition_Result` style.
- If a change affects the embedded web frontend, run the relevant `npm` tests or build path.

## Pull Requests
- Keep PRs scoped to one issue or one coherent change set.
- Include:
  - a short summary of the change
  - the commands you ran for verification
  - any config, environment, or migration notes
- Avoid mixing unrelated refactors with user-facing fixes.
- Update `CHANGELOG.md` under `[Unreleased]` for any user-facing additions, changes,
  fixes, or security improvements.

## Commit Messages
Use short conventional-style messages such as:
- `fix: verhindere plaintext-secret im settings api`
- `docs: aktualisiere launch-check`
- `refactor: vereinfache provider factory`

## Security
Do not open public issues for undisclosed vulnerabilities. Follow the guidance in [SECURITY.md](SECURITY.md).
