# EP-SMOKE-002 Fixture

## Purpose

EP-SMOKE-002 is a solar cooling smoke fixture scaffold.

It prepares future EnergyPlus comparison structure for a simplified south-facing window solar cooling case.

## Status

Current status:

    ReferenceFixturePlaceholder

Current comparison status after generic runner:

    PlaceholderComparison

This fixture is not a real EnergyPlus validation result yet.

## Case

Single-zone solar cooling smoke case.

Main simplified behavior:

    Q_solar = A_window * SHGC * I_surface

Fixture values:

- Window area = 8 m²
- SHGC = 0.55
- Peak incident solar = 650 W/m²
- Expected peak window solar gain = 2860 W
- Expected peak cooling load = 3600 W
- Expected daily cooling energy = 18.0 kWh

## Files

- tests/fixtures/validation/energyplus/EP-SMOKE-002/case-metadata.json
- tests/fixtures/validation/energyplus/EP-SMOKE-002/assistantengineer-input.json
- tests/fixtures/validation/energyplus/EP-SMOKE-002/reference-output.placeholder.json
- tests/fixtures/validation/energyplus/EP-SMOKE-002/comparison-tolerances.json

## Required non-claims

This fixture does not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- detailed EnergyPlus solar distribution parity.
