# EnergyPlus Real Fixture Intake Policy

Required files include source EnergyPlus model file, weather file, raw EnergyPlus output file, normalized reference output JSON and provenance metadata.

## Comparison requirements

NumericWithinTolerance, DirectionalTrend and SameSign.

This does not claim exact EnergyPlus numerical equivalence.

This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.

Use assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture.
