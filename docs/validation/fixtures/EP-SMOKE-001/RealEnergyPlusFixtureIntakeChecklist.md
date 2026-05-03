# EP-SMOKE-001 Real EnergyPlus Fixture Intake Checklist

Current state: PlaceholderComparison.

Future files:
- energyplus-model.idf
- weather.epw
- energyplus-output.raw.csv
- energyplus-output.reference.json
- provenance.json

Record EnergyPlus version.

Set referenceStatus = RealEnergyPlusOutput.

The comparison script reads real reference output when available.

Run assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture.

This does not claim exact EnergyPlus numerical parity or ASHRAE 140 validation coverage.
