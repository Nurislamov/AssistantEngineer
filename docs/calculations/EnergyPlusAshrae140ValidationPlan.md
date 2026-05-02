# EnergyPlus / ASHRAE 140 Validation Plan

## Purpose

This document defines the future validation direction for AssistantEngineer engineering-core calculations.

EnergyPlus and ASHRAE 140 are future validation layers. They are not required gates for engineering-core v1 formula closure.

The purpose of this validation layer is comparative engineering validation, not exact watt-by-watt parity.

## Validation philosophy

AssistantEngineer engineering-core v1 is an engineering calculation kernel, not an EnergyPlus clone.

Validation should answer:

- Are loads and annual energy results in an acceptable engineering range?
- Are signs and seasonal behavior correct?
- Are solar, internal gains, ventilation, infiltration and transmission sensitivities reasonable?
- Are warnings and assumptions visible to the user?
- Are simplified models clearly documented?

Validation should not claim:

- exact EnergyPlus numerical parity;
- full ASHRAE 140 compliance;
- full ISO 52016 node/matrix solver parity;
- full dynamic HVAC plant simulation parity.

## Proposed validation stages

### Stage 1 — smoke validation cases

Small deterministic cases:

- no windows;
- no internal gains;
- no ventilation;
- pure transmission heating case;
- pure solar cooling case;
- constant outdoor temperature;
- constant internal gains;
- one-room single-zone model.

Expected result:

- directionally correct heating/cooling;
- no negative impossible values;
- stable monthly/annual aggregation;
- diagnostics show assumptions.

### Stage 2 — simplified EnergyPlus comparison

Build a small EnergyPlus model that intentionally matches the simplified assumptions as closely as practical:

- one zone;
- simple box geometry;
- fixed schedules;
- simple construction U-values;
- ideal loads HVAC;
- no detailed plant;
- no latent/moisture validation;
- same weather file where possible.

Expected result:

- annual heating/cooling within documented tolerance;
- peak load within documented tolerance;
- monthly seasonal behavior matches directionally;
- differences are documented.

### Stage 3 — ASHRAE 140-style comparative cases

Use ASHRAE 140-style thinking:

- lightweight/heavyweight envelope sensitivity;
- window orientation sensitivity;
- solar gain sensitivity;
- infiltration/ventilation sensitivity;
- internal gain sensitivity;
- thermal mass sensitivity.

Expected result:

- not full certification;
- comparative validation report;
- tolerances and deviations documented;
- known model limitations listed.

## Suggested tolerances

Initial engineering tolerances should be conservative and explicitly documented.

Possible starting tolerances:

| Metric | Initial tolerance |
|---|---|
| Annual heating energy | ±20% |
| Annual cooling energy | ±20% |
| Monthly heating/cooling trend | same seasonal direction |
| Peak heating load | ±25% |
| Peak cooling load | ±25% |
| Solar orientation sensitivity | directionally correct |
| Free-floating temperature trend | directionally correct |

These tolerances are placeholders for future validation work. They must be reviewed after the first EnergyPlus comparison cases.

## Required metadata per validation case

Each validation case should include:

- case id;
- case name;
- validation source;
- weather source;
- geometry assumptions;
- envelope assumptions;
- ventilation/infiltration assumptions;
- internal gains assumptions;
- solar/shading assumptions;
- HVAC control assumptions;
- expected result;
- actual result;
- tolerance;
- pass/fail;
- known differences;
- notes.

## Recommended folder structure

Suggested future structure:

```text
tests/AssistantEngineer.Tests/Validation/EnergyPlus/
  Cases/
    Case001_SingleZone_TransmissionOnly/
    Case002_SingleZone_SolarCooling/
    Case003_SingleZone_InternalGains/
  Fixtures/
  Reports/
  EnergyPlusValidationCaseTests.cs