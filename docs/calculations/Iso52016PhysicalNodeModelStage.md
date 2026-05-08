# AE-ISO52016-002 Step 01 - ISO52016-inspired physical node model builder

## Status

This stage adds the first physical-node model builder layer on top of the existing `Iso52016MatrixHourlySolver`.

Claim boundary:

- ISO52016-inspired physical node model builder stage.
- validation/internal engineering anchors only.
- not complete ISO 52016 numerical equivalence.
- not StandardReference numerical equivalence.
- not EnergyPlus numerical equivalence.
- not ASHRAE Standard 140 benchmark-grade claim.

## Purpose

The Matrix solver already handles nodes, internal conductance links, boundary conductance links, hourly boundary temperatures, hourly gains, and HVAC control of the air node.

This step does not introduce a new solver. It introduces contracts and a builder that converts an already prepared hourly room input profile into a deterministic multi-node Matrix request.

## Initial node topology

Step 01 creates a stable three-node room model:

1. air node;
2. aggregated internal surface node;
3. aggregated thermal mass node.

Initial links:

- air <-> internal surface conductance;
- internal surface <-> thermal mass conductance;
- internal surface <-> outdoor boundary;
- internal surface <-> ground boundary;
- internal surface <-> adjacent-zone boundary;
- air <-> ventilation-air boundary when ventilation conductance is positive.

## Gains distribution

The builder splits gains into Matrix node heat gains:

- internal gains use a convective/radiative split;
- convective internal gains go to the air node;
- radiative internal gains are distributed between internal surface and thermal mass;
- solar gains can be split to air and then distributed between internal surface and thermal mass.

The defaults are deterministic engineering anchors for this stage, not external validation.

## Verification

Use:

```powershell
.\scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1
```

The script checks required source/docs/manifest/test files, guards forbidden positive equivalence claims, and runs the physical node model tests.

## Next steps

Later steps can expand the aggregate surface/mass model into explicit surface and construction-layer node adapters, add richer boundary profiles, and integrate operation schedules. Those steps must keep the same claim boundary unless external numerical validation is added.