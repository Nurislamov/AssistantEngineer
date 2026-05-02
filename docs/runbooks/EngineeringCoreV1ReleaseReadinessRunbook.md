# Engineering Core V1 Release Readiness Runbook

## Purpose

This runbook defines the final release readiness gate for Engineering Core V1.

Engineering Core V1 can be announced as closed only as an engineering formula gate, not as exact external-simulator parity.

## Release readiness command

Run from repository root:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

For a faster pre-check:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1 -Fast

When frontend dependencies are unavailable:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1 -SkipFrontend

When checking before a commit with intentional uncommitted changes:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1 -SkipGitStatus

## What the readiness gate verifies

The readiness gate verifies:

- required release artifacts exist;
- generated artifacts can be regenerated;
- frontend build passes unless skipped;
- smoke verification profile passes;
- contracts verification profile passes;
- manifest verification passes;
- full Engineering Core V1 verification passes unless Fast is used;
- full backend test suite passes unless skipped/Fast;
- git status is printed unless skipped.

## Required artifacts

The readiness gate expects:

- release manifest;
- release checklist;
- owner handoff;
- release evidence report;
- traceability matrix;
- diagnostics catalog;
- API contract snapshots;
- report contract snapshots;
- validation registry;
- validation readiness report;
- CI workflow;
- verification scripts;
- test profile scripts.

## Release-ready means

Release-ready means:

- FormulaAuditMatrix contains no unclosed v1 formula gates;
- Engineering Core status is ClosedV1;
- EPW/PVGIS and annual 8760 gates are closed;
- diagnostics Error/Warning/Info rules are protected;
- report calculationDisclosure is visible;
- frontend status/disclosure/diagnostics panels are visible;
- generated contracts and snapshots are present;
- release evidence and traceability matrix are generated;
- validation registry exists as future planned validation;
- CI and contribution guards exist.

## Release-ready does not mean

Release-ready does not mean:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in v1.

## Recommended final sequence

Before declaring Engineering Core V1 closed:

    .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1
    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1
    git status
    git tag engineering-core-v1

Tagging is optional, but recommended after the readiness gate is green.

## Failure handling

If the gate fails:

1. read the first failing step;
2. run the narrower profile directly;
3. fix the source artifact, not only the generated output;
4. regenerate artifacts;
5. rerun readiness gate.

Do not fix failures by weakening non-claims, hiding warnings, removing disclosure or deleting guard tests.
