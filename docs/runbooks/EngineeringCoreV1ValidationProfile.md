# Engineering Core V1 Validation Profile

## Purpose

The validation profile isolates the EnergyPlus / ASHRAE 140-style validation infrastructure from the main engineering-core smoke and contracts profiles.

Use it when changing:

- validation registry;
- validation fixtures;
- EnergyPlus placeholder or real reference outputs;
- comparison tolerances;
- validation runner scripts;
- validation generated reports;
- fixture catalog;
- fixture authoring templates.

## Main command

Run:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1

This regenerates validation artifacts and runs validation guard tests.

## Faster command when generated artifacts are already fresh

Run:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1 -SkipRegenerate

## Strict future mode for real EnergyPlus references

Run:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1 -RequireRealReferences

This requires real reference files instead of placeholder reference outputs.

Current repository status is still PlaceholderComparison, so strict mode is intended for a future milestone.

## Regenerate validation artifacts only

Run:

    .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1

This generates:

- EngineeringCoreV1ValidationReadiness.md;
- EP-SMOKE-001 comparison readiness;
- EP-SMOKE-001/002/003 comparison results;
- EngineeringCoreV1ValidationComparisonSummary;
- EnergyPlusValidationGenericComparisonSummary;
- EP-SMOKE-001 real fixture readiness;
- EnergyPlusValidationFixtureCatalog.

## CI workflow

Validation profile CI:

    .github/workflows/engineering-core-v1-validation.yml

The workflow fails if generated validation artifacts are stale.

## Current validation fixtures

Current placeholder fixtures:

- EP-SMOKE-001: transmission-only heating;
- EP-SMOKE-002: solar cooling;
- EP-SMOKE-003: internal gains cooling.

Current comparison status:

    PlaceholderComparison

## What this profile verifies

The profile verifies:

- registry structure;
- readiness reports;
- fixture scaffolds;
- placeholder references;
- comparison tolerances;
- generic fixture runner;
- per-case comparison outputs;
- summary reports;
- real fixture intake gate;
- fixture catalog synchronization;
- authoring kit templates and scaffold script.

## Non-claims

Passing this profile does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- latent/moisture/humidity support in V1.

Passing this profile means validation infrastructure is internally consistent and ready for future real EnergyPlus fixtures.
