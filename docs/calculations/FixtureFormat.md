# Fixture Format

Energy Calculation Parity fixtures are JSON files under:

```text
tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures
```

## Required Metadata

```json
{
  "fixtureName": "Example",
  "description": "What the fixture verifies.",
  "referenceType": "InternalDeterministic",
  "method": "Energy Calculation Parity / Function Name",
  "category": "RoomLoad",
  "input": {},
  "expected": {},
  "tolerances": {},
  "assumptions": [],
  "notes": []
}
```

## Reference Types

- `InternalDeterministic`
- `BenchmarkComparison`
- `ExternalReference`

`ExternalParityCovered` must not be used without a documented benchmark, tolerance and passing comparison test.

## Tolerances

Use the smallest tolerance that is stable for the calculation precision. Typical fields are:

- `hourlyLoadW`
- `monthlyDemandKWh`
- `annualDemandKWh`
- `hourlyTemperatureC`

## Diagnostics

Fixtures can list expected diagnostic codes. Diagnostics should identify missing assumptions, fallback behavior, clamped values and invalid inputs.
