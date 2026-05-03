# EnergyPlus Validation Tool Architecture

## Purpose

EnergyPlus validation automation must live in C# tools.

PowerShell scripts under `scripts/engineering-core` are thin wrappers only.

## Structure

- `tools/AssistantEngineer.Tools.EnergyPlusValidation` owns validation generation/comparison logic.
- `scripts/engineering-core` exposes local/CI entry points.
- `docs/reports/validation` contains generated evidence.
- `tests/fixtures/validation/energyplus` contains committed fixtures.
- `tests` contains guard tests.

## Commands owned by the C# tool

- compare fixtures;
- assert EP-SMOKE-001 real fixture readiness;
- generate fixture catalog;
- generate comparison summary;
- generate validation readiness;
- generate validation evidence;
- regenerate validation artifacts;
- run validation verification.

## Non-claims

This tool does not claim exact EnergyPlus numerical parity.

This tool does not claim ASHRAE 140 validation coverage.

This tool does not claim full ISO 52016 node/matrix solver parity.

Real validation must remain tolerance-based and provenance-backed.
