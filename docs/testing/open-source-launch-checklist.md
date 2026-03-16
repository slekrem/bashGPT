# Open-Source Launch Checklist

Last validated: March 16, 2026

## Goal
Provide a minimal, reproducible launch check for an external developer on a clean machine.

## Prerequisites
- .NET 9 SDK
- Node.js >= 20.19.0 or >= 22.12.0
- Ollama available locally if chat/server runtime should be exercised beyond build/test

## Reproducible Check
Run from the repository root:

```bash
dotnet build -m:1 /nodeReuse:false
dotnet test -m:1 /nodeReuse:false
```

Optional focused web validation:

```bash
cd src/bashGPT.Web
npm test
```

## README Spot Checks
- CLI build command in `README.md` matches the real build path.
- CLI examples use the Ollama-only provider model.
- Server examples match the current flags: `--provider`, `--model`, `--port`, `--no-browser`, `--verbose`.
- README documents the server tool trust boundary and `BASHGPT_SERVER_ALLOWED_TOOLS`.

## Go/No-Go Criteria
- Go:
  - `dotnet build` succeeds on a clean checkout.
  - `dotnet test` succeeds on a clean checkout.
  - The embedded web frontend builds as part of the server build.
  - README setup steps match the actual CLI/server UX.
- No-Go:
  - Clean build/test requires undocumented manual fixes.
  - Server build and CI use materially different frontend install flows.
  - Public-facing docs still describe removed providers or dead setup paths.
  - Open-source baseline files like license/community docs are still missing.

## Current Status
- Verified on March 16, 2026:
  - `dotnet build -m:1 /nodeReuse:false`: passed
  - `dotnet test -m:1 /nodeReuse:false`: passed
  - `src/bashGPT.Web` build path is reproducible via stamp-based `npm ci`
  - README matches the Ollama-only setup and current server flags

## Known Launch Blockers Outside This Check
- License and community-health files are still tracked separately in `#154`.
- GitHub/release preparation is still tracked separately in `#152`.
