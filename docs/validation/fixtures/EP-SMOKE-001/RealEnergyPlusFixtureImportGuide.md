# EP-SMOKE-001 Real EnergyPlus Fixture Import Guide

## Purpose

This guide documents how to import the first real EnergyPlus reference fixture for EP-SMOKE-001.

The import produces the committed files required by the real fixture intake gate:

- `energyplus-model.idf`
- `weather.epw`
- `energyplus-output.raw.csv`
- `energyplus-output.reference.json`
- `provenance.json`

## Non-claims

Importing this fixture does not claim exact EnergyPlus numerical parity.

Importing this fixture does not claim ASHRAE 140 validation coverage.

Importing this fixture does not claim full ISO 52016 node/matrix solver parity.

The comparison remains tolerance-based.

## Expected source directory

Prepare a source directory that contains:

```text
energyplus-model.idf
weather.epw
energyplus-output.raw.csv
```

The raw CSV must contain EnergyPlus result columns for heating energy and peak heating rate/load, or you must pass the normalized metric values explicitly.

## Import with explicit metric values

```powershell
.\scripts\engineering-core\import-ep-smoke-001-real-fixture.ps1 `
    -SourceDirectory "D:\EnergyPlusRuns\EP-SMOKE-001" `
    -EnergyPlusVersion "24.1.0" `
    -AnnualHeatingEnergyKwh 37.8 `
    -PeakHeatingLoadW 1575 `
    -AnnualCoolingEnergyKwh 0
```

Use explicit values only when they are copied from a real EnergyPlus run/output report.

Do not use AssistantEngineer expected values as fake EnergyPlus reference values.

## Import by reading CSV columns

```powershell
.\scripts\engineering-core\import-ep-smoke-001-real-fixture.ps1 `
    -SourceDirectory "D:\EnergyPlusRuns\EP-SMOKE-001" `
    -EnergyPlusVersion "24.1.0" `
    -HeatingEnergyColumn "DistrictHeating:Facility [J](Hourly)" `
    -HeatingLoadColumn "Zone Ideal Loads Supply Air Total Heating Rate [W](Hourly)" `
    -CoolingEnergyColumn "DistrictCooling:Facility [J](Hourly)"
```

The script can infer common EnergyPlus column names, but passing explicit column names is safer.

## Verification

After import, run:

```powershell
.\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture
.\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences
.\scripts\engineering-core\verify-engineering-core-v1-validation.ps1
```

Then verify the working tree:

```powershell
git status -sb
```

Expected result after committing generated artifacts:

```text
EP-SMOKE-001 = RealEnergyPlusComparison
```
