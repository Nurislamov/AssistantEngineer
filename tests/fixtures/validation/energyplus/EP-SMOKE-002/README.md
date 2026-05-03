# EP-SMOKE-002

Status: `ReferenceFixturePlaceholder`

This smoke fixture represents a cooling and solar-orientation placeholder scenario for the EnergyPlus validation harness.

## Purpose

The fixture verifies that the generic validation runner can read:

- `case-metadata.json`
- `assistantengineer-input.json`
- `reference-output.placeholder.json`
- `comparison-tolerances.json`

and produce comparison JSON/Markdown outputs.

## Non-claims

- This is not a real EnergyPlus validation result yet.
- This does not claim exact EnergyPlus numerical parity.
- This does not claim exact pyBuildingEnergy numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- Future real validation must replace or supplement the placeholder reference with provenance-backed EnergyPlus output.