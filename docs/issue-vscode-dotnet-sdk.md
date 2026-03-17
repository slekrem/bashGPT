# Issue: VS Code Run/Debug fails due to missing .NET SDK 9.0.301

## Symptom
When running `bashGPT.Server` or `bashGPT.Cli` via VS Code "Run and Debug", the process fails because the configured .NET SDK (9.0.301) is not found, even though it is installed in `~/.dotnet`.

## Root Cause
The VS Code launch/task configurations don't set `PATH` / `DOTNET_ROOT` to include the local `~/.dotnet` where the required SDK is installed.

## Fix
- Update `.vscode/launch.json` and `.vscode/tasks.json` so the environment includes:
  - `PATH=${env:HOME}/.dotnet:${env:PATH}`
  - `DOTNET_ROOT=${env:HOME}/.dotnet`

## Notes
This is needed because some systems (e.g. CI containers, developer machines) may not have the matching SDK globally installed.
