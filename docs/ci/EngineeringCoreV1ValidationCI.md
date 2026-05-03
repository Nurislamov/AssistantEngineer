# Engineering Core V1 Validation CI

## Purpose

This CI workflow runs the validation profile separately from smoke/contracts/full release gates.

Workflow:

    .github/workflows/engineering-core-v1-validation.yml

Command:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1

## Trigger paths

The workflow runs on validation-related changes:

- docs/validation/**
- docs/reports/validation/**
- tests/fixtures/validation/**
- tests/AssistantEngineer.Tests/Validation/**
- scripts/engineering-core/*validation*
- scripts/engineering-core/*energyplus*
- scripts/engineering-core/*ep-smoke*

## Stale artifact protection

The workflow runs validation generation and fails if generated validation artifacts are stale.

Developer command:

    .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1

## Current status

Current validation status:

    PlannedValidation

Current fixture comparison status:

    PlaceholderComparison

## Non-claims

CI success does not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.

CI success only means validation infrastructure, placeholder comparisons, catalog sync and guard tests passed.
