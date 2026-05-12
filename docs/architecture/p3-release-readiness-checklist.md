# P3 Release Readiness Checklist

## Purpose

This checklist is the final P3 regression gate reference.
It confirms build, tests, release-readiness verification, and governance boundaries after the P3 hardening cycle.

## Mandatory Backend Gates

Run from repository root:

```powershell
dotnet build AssistantEngineer.sln -c Debug --no-restore
dotnet test AssistantEngineer.sln -c Debug --no-restore --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1
```

## P3 Verify Wrappers

Run from repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-13-building-input-validation-refactor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-14-energy-calculation-pipeline-refactor.ps1
```

## Frontend Gates

Run from `src/Frontend`:

```powershell
npm run test
npm run test:e2e
```

## Working Tree Hygiene

- `git status`
- `git diff --stat`
- ensure no generated artifacts are committed:
  - `playwright-report/`
  - `test-results/`
  - `blob-report/`
  - `node_modules/`
  - temporary `*.bak`, `*.orig`, `*.tmp` files

## Governance and Non-claims

The P3 closure must preserve explicit non-claims:

- no exact EnergyPlus numerical equivalence claim;
- no exact ASHRAE 140 / BESTEST-style validation coverage claim;
- no full ISO/EN compliance claim;
- no external certification claim;
- no production-complete distributed execution claim.

Final status/audit references:

- `docs/architecture/p3-hardening-status.md`
- `docs/architecture/p3-hardening-summary.md`
- `docs/architecture/p3-final-architecture-audit.md`

## Known Out-of-scope Items (Post-P3)

- distributed stale-lease recovery;
- dead-letter queue;
- advanced retry/backoff scheduler;
- distributed idempotency acceleration;
- object/blob storage for large artifacts;
- broader OpenAPI governance automation;
- large-scale production-data performance benchmark campaign;
- external numerical validation beyond internal deterministic anchors.
