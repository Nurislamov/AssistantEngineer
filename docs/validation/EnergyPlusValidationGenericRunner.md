# Generic EnergyPlus Validation Fixture Runner

Script: compare-energyplus-validation-fixtures.ps1.

Supports -RequireRealReferences.

Reads reference-output.placeholder.json and energyplus-output.reference.json.

Metric types: NumericWithinTolerance, SameSign, DirectionalTrend.

Includes EP-SMOKE-001, EP-SMOKE-002 and EP-SMOKE-003.

PlaceholderComparison is not real EnergyPlus validation.

Future real validation must remain tolerance-based.
