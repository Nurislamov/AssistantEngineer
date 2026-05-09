# Domestic Hot Water EN12831-3-Style Calculation

## Purpose

AssistantEngineer provides an EN12831-3-style standard-based domestic hot water calculation lane as an internal analytical anchor.

This lane is not a full validation package and does not claim full EN12831-3 compliance.

## Supported Usage Categories

- residential dwelling
- office
- school
- hotel
- healthcare
- generic fallback

## Table-Driven Approach

- usage category resolves a reference table entry with deterministic liters-per-driver defaults;
- driver is selected by reference mode (`PeopleBased`, `AreaBased`, `UnitBased`, `CustomVolume`);
- equivalent occupants can be derived from people count and category factor;
- draw profile can be category-derived and used for optional 8760 hourly distribution.

## Explicit Input Override

- explicit liters-per-driver inputs override category defaults;
- `CustomVolume` overrides table-derived daily volume;
- compatibility path remains unchanged unless the ISO12831-inspired path is explicitly enabled.

## Outputs

- daily volume and draw energy
- daily total energy (with losses)
- monthly volume and energy
- annual volume and energy
- optional hourly volume and energy distribution

## Limitations

- no full EN12831-3 compliance claim;
- no external validation claim;
- no plant/system efficiency modeling in this lane;
- no stochastic occupant behavior model;
- no latent/moisture domestic hot water model.

## Fixtures

- `tests/fixtures/dhw/en12831/residential-table-driven.json`
- `tests/fixtures/dhw/en12831/office-table-driven.json`
- `tests/fixtures/dhw/en12831/school-table-driven.json`
- `tests/fixtures/dhw/en12831/explicit-volume-override.json`
