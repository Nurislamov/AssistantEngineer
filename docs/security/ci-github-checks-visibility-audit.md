# CI/GitHub Checks Visibility Audit

## Purpose

This audit defines CI/GitHub checks visibility expectations for AssistantEngineer governance and release-ready signals, without changing runtime behavior or write-path boundaries.

## Scope

- `.github/workflows` inventory and trigger coverage;
- expected check visibility for build/test/release-ready gates;
- release-ready signal relationship in GitHub checks;
- gaps between expected and observable commit-status behavior;
- safe CI observability improvements that do not weaken gates.

## Non-claims

- No production security certification claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No staging apply execution claim.
- No full multi-tenant isolation claim yet.
- No DB row-level security claim.
- No global EF query filter claim.
- No certified/certification claim.

## Current GitHub checks visibility

- GitHub Actions workflows are present under `.github/workflows`.
- Multiple Engineering Core verification workflows are configured.
- Release-ready is represented by a dedicated workflow (`engineering-core-v1-release-ready.yml`) with tag trigger + manual trigger.
- Commit-status visibility can still appear empty for specific commits when no matching workflow trigger fired for that commit context.

## Current workflow inventory

- `.github/workflows/engineering-core-v1.yml`
- `.github/workflows/engineering-core-v1-smoke.yml`
- `.github/workflows/engineering-core-v1-contracts.yml`
- `.github/workflows/engineering-core-v1-validation.yml`
- `.github/workflows/engineering-core-v1-release-ready.yml`
- `.github/workflows/iso52016-matrix-release-ready.yml`

## Expected CI visibility contract

Required checks:

- `dotnet build AssistantEngineer.sln -c Debug` (direct step or script-contained gate);
- `dotnet test AssistantEngineer.sln -c Debug` (direct step or script-contained gate);
- `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`.

Optional checks:

- frontend-only checks when frontend/tooling context is applicable;
- release-ready summary artifact publication;
- test-result/coverage artifacts in future stages.

## Release-ready relationship

- Release-ready gate remains a hard gate and does not downgrade failures to warnings.
- P7-04 observability improvements (stage timing, deterministic summary, safe failure diagnostics) are now consumable in CI via summary JSON output.
- CI visibility contract must surface release-ready status without exposing secrets.

## Required checks

- Backend build.
- Backend tests.
- Engineering Core release-ready gate.
- Ownership backfill apply-disabled boundary coverage through `dotnet test` suite.

## Optional checks

- Frontend build/test profiles.
- Workflow-level summary artifacts for trend analysis.
- Additional non-blocking telemetry dashboards.

## GitHub status limitations

- Checks can be absent for commits when branch/tag/path filters do not match workflow triggers.
- Checks can be absent for commits when workflows are not permitted to run in repository settings/forks.
- Tag-only release-ready workflow does not emit checks for non-tag pushes by design.

## Observability gaps

- No single documented operator runbook existed for interpreting empty/missing check statuses.
- Workflow status-to-gate mapping was not centralized in one governance document.
- Some workflow diagnostics were visible only in raw logs instead of a standardized summary contract.

## Implemented improvements

- Added this CI visibility audit and explicit required/optional checks contract.
- Added CI visibility runbook for status interpretation and local fallback verification.
- Added governance tests covering CI visibility docs and workflow safety constraints.
- Improved `engineering-core-v1-release-ready.yml` to publish release-ready summary JSON into workflow summary/artifact (safe metadata only).

## Remaining limitations

- Visibility still depends on repository settings and GitHub permissions outside source control.
- Workflow status completeness can vary by trigger context (push/pr/tag/manual).
- No claim is made that every commit in every branch will always show identical checks.

## Next steps

- P7-06: route inventory and claims consistency automation.
- Optional future: add consolidated CI dashboard artifact index for trend history.
