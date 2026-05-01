# Fixture Format

Energy Calculation Parity fixtures are JSON files under:

```text
tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures
```

Benchmark comparison fixtures are JSON files under:

```text
tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/BenchmarkFixtures/Fixtures
```

## Required Metadata

```json
{
  "fixtureName": "Example",
  "description": "What the fixture verifies.",
  "category": "AnnualEnergyBalance",
  "referenceType": "BenchmarkReference",
  "method": "Energy Calculation Parity / Function Name",
  "status": "Active",
  "input": {},
  "expected": {
    "annualHeatingDemandKWh": 8760,
    "componentBreakdown.netTransmissionKWh": -876
  },
  "tolerances": {
    "defaultAbsolute": 0.001,
    "defaultRelativePercent": 0.1,
    "fields": {
      "annualHeatingDemandKWh": {
        "absolute": 0.01,
        "relativePercent": 0.1
      }
    }
  },
  "assumptions": [],
  "notes": []
}
```

## Reference Types

- `InternalDeterministic`: expected values are derived from AssistantEngineer deterministic assumptions and formula-level arithmetic.
- `BenchmarkReference`: expected values are fixed benchmark/reference values used by comparison tests. This can be an internal benchmark fixture and does not automatically prove external parity.
- `ExternalReference`: expected values come from a documented external benchmark/reference source.

`ExternalParityCovered` must not be used without a documented benchmark, tolerance and passing comparison test.

## Fixture Status

- `Active`: loaded and executed by benchmark comparison tests.
- `Pending`: valid fixture draft, skipped by default and reported by the loader.
- `Disabled`: intentionally skipped by default and reported by the loader.

## Categories

Supported fixture category values are:

- `Transmission`
- `SolarGains`
- `VentilationInfiltration`
- `InternalGains`
- `RoomLoad`
- `Aggregation`
- `AnnualEnergyBalance`
- `Dhw`
- `SystemEnergy`
- `EquipmentSizing`
- `HourlyEnergyBalance`
- `SignedComponentBalance`

The first benchmark runner supports `AnnualEnergyBalance` and `SignedComponentBalance`.

## Tolerances

Use the smallest tolerance that is stable for the calculation precision.

Benchmark tolerances support:

- `defaultAbsolute`
- `defaultRelativePercent`
- per-field `absolute`
- per-field `relativePercent`

A benchmark field passes when either the absolute difference is within absolute tolerance or the relative difference percent is within relative tolerance. Expected zero is handled without division by zero; a nonzero actual value against expected zero must pass through absolute tolerance.

Legacy deterministic fixtures still use their existing typed tolerance fields, such as `hourlyLoadW`, `monthlyDemandKWh`, `annualDemandKWh`, and `hourlyTemperatureC`.

## Expected Value Paths

Benchmark `expected` values use result field paths. The paths are matched case-insensitively against the actual result object:

```json
{
  "expected": {
    "annualHeatingDemandKWh": 8760,
    "annualCoolingDemandKWh": 0,
    "componentBreakdown.netTransmissionKWh": -876,
    "isTrueHourly8760": true
  }
}
```

Numeric values are compared with tolerances. Boolean values are compared by exact equality.

## Diagnostics

Fixtures can list expected diagnostic codes. Diagnostics should identify missing assumptions, fallback behavior, clamped values and invalid inputs.

## Adding A Benchmark Fixture

1. Add a JSON file under `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/BenchmarkFixtures/Fixtures`.
2. Set `referenceType` to `BenchmarkReference` for fixed benchmark/reference expected values, or `ExternalReference` only when the source is documented.
3. Set `status` to `Active` only when the expected values and tolerances are ready to execute.
4. Add compact input data and expected field paths.
5. Add strict tolerances for deterministic arithmetic.
6. Include assumptions and notes. For external references, include source/version details in the notes or source reference.

`BenchmarkCompared` means AssistantEngineer was compared against fixed expected benchmark/reference values in a passing test. It does not automatically mean `ExternalParityCovered`.
