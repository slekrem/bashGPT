# Public Launch Runbook

Last updated: March 16, 2026

## Goal
Provide a predictable maintainer workflow for making the repository public and publishing the first release.

## Suggested First Release
- Tag: `v0.1.0`
- Scope: first public open-source release of the Ollama-only codebase

## Pre-Launch Checklist
- Community health files exist:
  - `LICENSE`
  - `CONTRIBUTING.md`
  - `CODE_OF_CONDUCT.md`
  - `SECURITY.md`
- README reflects the current CLI, server, and Ollama-only setup
- Command-line host dependency is on a stable `System.CommandLine` release
- Open-source launch check has passed:
  - `dotnet build -m:1 /nodeReuse:false`
  - `dotnet test -m:1 /nodeReuse:false`
- Remaining launch blockers are explicitly resolved or accepted
- Repository description, topics, and homepage are set in GitHub

## Release Candidate Flow
1. Merge all launch-blocking issues into `main`.
2. Run the full build and test check from a clean checkout.
3. Verify artifact identity:
   - `dotnet run --project src/bashGPT.Cli -- --version`
   - start the server and check `GET /api/version`
   - both should report the intended release version/tag
4. Review the diff since the last internal milestone and summarize user-visible changes.
5. Create tag `v0.1.0` from the exact release commit.
6. Draft GitHub release notes from the merged PRs and milestone issues.

## Recommended Release Notes Structure
- Highlights
- Breaking or notable behavior changes
- Setup requirements
- Known limitations

## Public Toggle Checklist
Before switching the repository to public:
- Confirm repository description is accurate
- Add or verify topics relevant to `dotnet`, `cli`, `ollama`, `agent`, `tool-calling`
- Verify Actions permissions and default branch protections
- Verify security reporting path in `SECURITY.md`
- Verify issue and PR templates are active

## Go-Live Day Steps
1. Pull latest `main`.
2. Re-run:
   ```bash
   dotnet build -m:1 /nodeReuse:false
   dotnet test -m:1 /nodeReuse:false
   ```
3. Create and push release tag:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```
4. Publish the GitHub release with curated notes.
5. Switch repository visibility to public.
6. Verify public-facing pages:
   - README renders correctly
   - license is detected by GitHub
   - issue and PR templates are available
   - release page is visible

## Go / No-Go Criteria
- Go:
  - build and test are green on the release commit
  - repo metadata and community files are complete
  - first release tag and notes are prepared
- No-Go:
  - unresolved launch blocker issues remain
  - build/test only works with undocumented local fixes
  - public-facing docs contradict the shipped behavior

## Notes
- `System.CommandLine` is pinned to the stable `2.0.2` release for both host projects. No preview-only APIs are used in the current CLI or server entry points.
- Repository metadata like description, topics, homepage, and visibility cannot be enforced from this repository alone; they must be set in GitHub UI or via `gh repo edit`.
