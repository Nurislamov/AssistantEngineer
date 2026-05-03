# Engineering Core V1 CI Profiles

## Purpose

Engineering Core V1 has many guard layers.

This document defines how CI should run those layers without making every pull request unnecessarily heavy.

## Workflows

### Smoke workflow

File:

    .github/workflows/engineering-core-v1-smoke.yml

Command:

    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1

Purpose:

- fast pull-request gate;
- frontend build;
- formula audit smoke;
- status/disclosure smoke;
- diagnostics API smoke;
- frontend visibility smoke;
- EPW/PVGIS/annual 8760 smoke;
- hourly heat-balance closure smoke.

### Contracts workflow

File:

    .github/workflows/engineering-core-v1-contracts.yml

Command:

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1

Purpose:

- generated artifacts;
- API snapshots;
- OpenAPI contract;
- report snapshots;
- export disclosure policy;
- diagnostics catalog;
- release evidence;
- traceability matrix;
- validation registry;
- documentation guards.

This workflow fails when generated artifacts are stale.

### Release readiness workflow

File:

    .github/workflows/engineering-core-v1-release-ready.yml

Command:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

Purpose:

- final release gate;
- manual workflow_dispatch;
- tag-based release gate for engineering-core-v1* tags.

## Existing full workflow

The original workflow remains:

    .github/workflows/engineering-core-v1.yml

It can continue to run the full verification command:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

The profile workflows make iteration faster while preserving full release coverage.

## Recommended PR policy

For normal PRs:

- Engineering Core V1 Smoke must pass.
- Engineering Core V1 Contracts must pass when docs/contracts/scripts/tests changed.

Before release:

- Engineering Core V1 Release Ready must pass.
- Full backend test suite must pass.
- Working tree must be clean after generated artifacts.

## Non-claims

CI success does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- latent/moisture/humidity support in V1.

CI success means the Engineering Core V1 formula gate, diagnostics, disclosures, contracts, traceability and release-readiness guards passed.

## Validation workflow

Validation workflow documentation:

    docs/ci/EngineeringCoreV1ValidationCI.md

Workflow file:

    .github/workflows/engineering-core-v1-validation.yml

Command:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1
