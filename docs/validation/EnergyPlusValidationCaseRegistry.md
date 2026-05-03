# EnergyPlus Validation Case Registry

Status: PlannedValidation.

This registry is not exact EnergyPlus parity and not ASHRAE 140 certification.

## Required case metadata

Each case includes source, weatherSource, geometry, envelope, internalGains, ventilation, hvacControl, assumptions, knownDifferences and nonClaims.

## Required metric metadata

Metrics use NumericWithinTolerance, DirectionalTrend and SameSign.

## Generation

Run generate-engineering-core-v1-validation-readiness.ps1.

Guarded by EnergyPlusValidationCaseRegistryTests.
