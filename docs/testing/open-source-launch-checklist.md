# Open-Source Launch Checklist

Last validated: March 18, 2026

## Goal
Provide a minimal, reproducible launch check for an external developer on a clean machine.

## Prerequisites
- .NET SDK 9.0.301 (`global.json`)
- Node.js 20.19.0 (same version as CI release checks)
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
  - Public launch metadata or release readiness is not documented well enough to execute the go-live cleanly.

## Current Status
- Verified on March 18, 2026:
  - `dotnet build -m:1 /nodeReuse:false`: passed
  - `dotnet test -m:1 /nodeReuse:false`: passed
  - `src/bashGPT.Web` build path is reproducible via stamp-based `npm ci`
  - README matches the Ollama-only setup and current server flags
  - Release baseline is pinned to `.NET SDK 9.0.301` and `Node.js 20.19.0`
  - License and community-health files are present in the repository root
  - Launch/release process documentation exists for maintainers

## Remaining Follow-Up
- No external launch blockers are currently tracked outside this checklist.
- Re-run this checklist before the public launch date and update the validation date with the latest results.
