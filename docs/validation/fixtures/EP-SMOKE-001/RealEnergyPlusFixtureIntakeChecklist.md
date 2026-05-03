# EP-SMOKE-001 Real EnergyPlus Fixture Intake Checklist

## Purpose

This checklist will be used when replacing or supplementing the EP-SMOKE-001 placeholder comparison with real EnergyPlus reference output.

## Current status

Current status:

    PlaceholderComparison

This means:

- fixture structure exists;
- placeholder reference output exists;
- comparison harness exists;
- real EnergyPlus model/output is not committed yet.

## Required future files

Before marking EP-SMOKE-001 as real fixture ready, add:

- [ ] tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-model.idf
- [ ] tests/fixtures/validation/energyplus/EP-SMOKE-001/weather.epw
- [ ] tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-output.raw.csv
- [ ] tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-output.reference.json
- [ ] tests/fixtures/validation/energyplus/EP-SMOKE-001/provenance.json

## Provenance checklist

provenance.json must document:

- [ ] EnergyPlus version;
- [ ] operating system;
- [ ] run date;
- [ ] source model file;
- [ ] weather file or synthetic weather definition;
- [ ] output variables used;
- [ ] unit conversions;
- [ ] known differences;
- [ ] tolerance policy;
- [ ] required non-claims.

## Reference output checklist

energyplus-output.reference.json must document:

- [ ] caseId = EP-SMOKE-001;
- [ ] referenceEngine = EnergyPlus;
- [ ] referenceStatus = RealEnergyPlusOutput;
- [ ] annualHeatingEnergyKwh or fixturePeriodHeatingEnergyKwh;
- [ ] peakHeatingLoadW;
- [ ] annualCoolingEnergyKwh or fixturePeriodCoolingEnergyKwh;
- [ ] source raw output file;
- [ ] unit conversion notes;
- [ ] non-claims.

## Comparison checklist

Before accepting the real fixture:

- [ ] comparison-tolerances.json has documented tolerances;
- [ ] comparison script reads real reference output when available;
- [ ] generated comparison result is not PlaceholderComparison;
- [ ] generated comparison report states tolerance-based comparison;
- [ ] non-claims remain visible;
- [ ] no exact EnergyPlus parity claim is introduced;
- [ ] no ASHRAE 140 validation coverage claim is introduced.

## Strict gate

When files are ready, run:

    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture

## Required non-claims

This fixture must not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.
