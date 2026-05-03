# EP-SMOKE-003 Fixture

## Purpose

EP-SMOKE-003 is an internal sensible gains cooling smoke fixture scaffold.

It prepares future EnergyPlus comparison structure for sensible-only internal gains.

## Status

Current status:

    ReferenceFixturePlaceholder

Current comparison status after generic runner:

    PlaceholderComparison

This fixture is not a real EnergyPlus validation result yet.

## Case

Single-zone internal gains cooling smoke case.

Main simplified behavior:

    Q_cooling = Q_internal_sensible

Fixture values:

- Internal sensible gain = 1200 W
- Duration = 24 h
- Expected daily cooling energy = 28.8 kWh
- Latent gain = 0 W

## Files

- tests/fixtures/validation/energyplus/EP-SMOKE-003/case-metadata.json
- tests/fixtures/validation/energyplus/EP-SMOKE-003/assistantengineer-input.json
- tests/fixtures/validation/energyplus/EP-SMOKE-003/reference-output.placeholder.json
- tests/fixtures/validation/energyplus/EP-SMOKE-003/comparison-tolerances.json

## Required non-claims

This fixture does not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- latent or moisture validation coverage.
