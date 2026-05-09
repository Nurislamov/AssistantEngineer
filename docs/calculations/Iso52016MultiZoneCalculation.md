# ISO52016-Style Multi-Zone Calculation

This document describes the current ISO52016-style standard-based multi-zone calculation path in AssistantEngineer.

AssistantEngineer is a standalone C# standard-based engineering calculation platform.

## Supported scope

The current multi-zone stage provides:

- multi-zone graph assembly and validation;
- coupled hourly zone-air solve for one hour or annual 8760-hour profiles;
- external boundary heat-transfer links;
- adjacent unconditioned boundary temperature links;
- adjacent conditioned same-use adiabatic-style boundaries (`IsAdiabaticEquivalent=true`);
- explicit inter-zone conductance links;
- per-zone internal gains and solar gains;
- per-zone ventilation/infiltration conductance to outdoor boundary context;
- per-zone heating/cooling setpoint control and zone-level hourly loads;
- building-level hourly and annual/monthly heating/cooling summaries.

## Unsupported scope

The current stage does not provide:

- no full ISO52016 compliance claim;
- no external validation claim coverage;
- no EnergyPlus validation-coverage claim;
- no ASHRAE 140 / BESTEST-style validation-coverage claim;
- no full coupled inter-zone airflow solver (airflow link remains a placeholder contract);
- no moisture or latent-load coupling claim;
- no detailed HVAC plant coupling claim.

## Known limitations

- The current coupled solver supports profile lengths of `1` or `8760` only.
- Same-use adiabatic behavior is represented as neutral transfer when `IsAdiabaticEquivalent=true`.
- Inter-zone airflow links are accepted by contracts/validation but are not yet solved in the hourly matrix.
- The current step is intentionally an internal engineering anchor for deterministic evolution of multi-zone behavior.

## Validation boundary

This stage is an internal engineering anchor and validation anchor.

Claim boundary:

- standard-based multi-zone calculation;
- deterministic internal analytical anchors;
- not full validation;
- no external-comparison coverage claim.

## Fixture set

Multi-zone internal analytical fixtures are stored in:

- `tests/fixtures/iso52016/multi-zone/two-zone-independent.json`
- `tests/fixtures/iso52016/multi-zone/two-zone-interzone-conductance.json`
- `tests/fixtures/iso52016/multi-zone/adjacent-unconditioned-zone.json`
- `tests/fixtures/iso52016/multi-zone/same-use-adiabatic-boundary.json`

## Single-zone compatibility

Existing single-zone ISO52016 calculation behavior remains unchanged.
