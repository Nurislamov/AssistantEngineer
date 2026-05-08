# EP-SMOKE-001

Status: `ReferenceFixturePlaceholder`

This smoke fixture represents a simple transmission-only heating placeholder scenario for the EnergyPlus validation harness.

## Purpose

The fixture verifies that the generic validation runner can read:

- `case-metadata.json`
- `assistantengineer-input.json`
- `reference-output.placeholder.json`
- `comparison-tolerances.json`

and produce comparison JSON/Markdown outputs.

## Formula

```text
Q = U * A * ΔT
```

Expected placeholder values:

```text
Expected transmission heat loss = 1575 W
Expected daily heating energy = 37.8 kWh
```

## Non-claims

- This is not a real EnergyPlus validation result yet.
- This does not claim exact EnergyPlus numerical equivalence.
- This does not claim exact StandardReference numerical equivalence.
- This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Future real validation must replace or supplement the placeholder reference with provenance-backed EnergyPlus output.