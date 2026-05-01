# Benchmark Fixtures

Benchmark fixtures are test-only JSON files for comparing AssistantEngineer calculation results with fixed expected benchmark/reference values.

Current location:

```text
tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/BenchmarkFixtures/Fixtures
```

The infrastructure is not production code and does not call external tools, web services or reference projects during normal `dotnet test`.

## Reference Types

- `InternalDeterministic`: expected values are derived from AssistantEngineer deterministic assumptions and arithmetic.
- `BenchmarkReference`: expected values are fixed benchmark/reference values used by comparison tests. They may be internal benchmark references.
- `ExternalReference`: expected values come from a documented external benchmark/reference source.

`BenchmarkReference` and `ExternalReference` both use the same comparison mechanism. Only `ExternalReference` with source/version evidence can support an `ExternalParityCovered` claim.

## Status

- `Active`: loaded and executed by default.
- `Pending`: skipped by default and reported by the loader.
- `Disabled`: skipped by default and reported by the loader.

Invalid JSON or missing required fields fail with a fixture-specific error.

## Tolerance Comparison

Numeric expected fields are compared by field path. A comparison passes when either condition is true:

- absolute difference is less than or equal to `absolute`
- relative difference percent is less than or equal to `relativePercent`

Field-specific tolerances override defaults:

```json
{
  "tolerances": {
    "defaultAbsolute": 0.001,
    "defaultRelativePercent": 0.1,
    "fields": {
      "annualHeatingDemandKWh": {
        "absolute": 0.01,
        "relativePercent": 0.1
      }
    }
  }
}
```

Expected zero is handled without division by zero. A nonzero actual value against expected zero must pass through absolute tolerance.

## Current Fixtures

- `annual-constant-heating-8760.json`: AnnualEnergyBalance, constant 1000 W heating for 8760 hours.
- `annual-constant-cooling-8760.json`: AnnualEnergyBalance, constant 500 W cooling for 8760 hours.
- `signed-component-balance-winter.json`: SignedComponentBalance, negative net transmission/ventilation/ground totals.
- `signed-component-balance-summer.json`: SignedComponentBalance, positive net transmission/ventilation/ground totals plus solar/internal gains.
- `signed-component-balance-with-infiltration-winter.json`: SignedComponentBalance, negative net ventilation and separate infiltration totals.

These fixtures are deterministic benchmark references. They are not `ExternalParityCovered`.

## Adding A Fixture

1. Add a JSON file under the benchmark fixture folder.
2. Fill required fields: `fixtureName`, `description`, `category`, `referenceType`, `method`, `status`, `input`, `expected`, `tolerances`, `assumptions`, and `notes`.
3. Use `Active` only after expected values and tolerances are fixed.
4. Add expected field paths that exist on the actual result object, such as `annualHeatingDemandKWh` or `componentBreakdown.netTransmissionKWh`.
5. Use strict tolerances for deterministic arithmetic.
6. For external references, document source name, version, source input, expected output and tolerance.

## Meaning Of BenchmarkCompared

`BenchmarkCompared` means a passing test compared AssistantEngineer results against fixed expected benchmark/reference values.

It does not automatically mean external parity.

`ExternalParityCovered` additionally requires:

- documented external source
- fixed input
- expected output
- tolerance
- passing comparison test
- source/version noted
