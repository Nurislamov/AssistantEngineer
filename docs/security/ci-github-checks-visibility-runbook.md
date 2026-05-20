# CI/GitHub Checks Visibility Runbook

## Purpose

Provide a safe operator runbook for interpreting GitHub checks visibility and fallback verification for AssistantEngineer governance gates.

## When to use

- GitHub checks for a commit appear empty, partial, or unclear.
- A reviewer needs to verify expected release-ready/build/test signals.
- Workflow logs exist but status interpretation is ambiguous.

## How to check latest commit status

1. Open the repository Actions tab.
2. Find workflows matching the branch/tag context.
3. Verify whether trigger conditions (branch/path/tag/manual) matched the commit.
4. Confirm whether required checks completed successfully or failed.

## Expected checks

- Backend build: `dotnet build AssistantEngineer.sln -c Debug`
- Backend tests: `dotnet test AssistantEngineer.sln -c Debug`
- Release-ready: `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`

## What empty GitHub statuses mean

- Trigger mismatch (branch/tag/path) for that commit.
- Workflow permissions/settings prevent execution.
- Commit context is outside configured workflow coverage.

Empty statuses are a visibility gap, not proof that checks passed.

## Local fallback verification

- `git status --short`
- `git log --oneline -10`
- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`
- Disabled apply boundary check:
  - `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence .\artifacts\ownership-backfill\dry-run-local --gate-result .\artifacts\ownership-backfill\gate-local\missing.json --output .\artifacts\ownership-backfill\apply-local --database-provider SQLite --connection-string "<connection-string>"`

## Release-ready summary

- Release-ready workflow can emit a deterministic summary JSON artifact (`artifacts/engineering-core/release-ready-summary.json`) and publish it to workflow summary.
- Summary contains stage/status/duration/exit-code metadata only.

## Failure triage

1. Identify failed workflow/job/step and trigger context.
2. Use release-ready deterministic summary for slow/failing stage location.
3. Reproduce locally with safe fallback commands.
4. Open remediation task with failing stage, command, and exit code.

## Safe logs policy

- Never log raw connection strings, passwords, tokens, API keys, or payload bodies.
- Error output may include argument names but not secret values.
- Preserve failure visibility; do not convert hard failures into warnings.

## Non-claims

- No production security certification claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No staging apply execution claim.
- No full multi-tenant isolation claim yet.
- No DB row-level security claim.
- No global EF query filter claim.
- No certified/certification claim.
