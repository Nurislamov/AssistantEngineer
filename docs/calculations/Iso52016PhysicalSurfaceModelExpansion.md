# AE-ISO52016-002 Step 02 вЂ” ISO52016-inspired physical surface/construction expansion

## Status

This stage expands the Step 01 physical node model builder with explicit surface and construction contracts.

Claim boundary:

- ISO52016-inspired physical surface/construction expansion.
- validation/internal engineering anchors only.
- not complete ISO 52016 numerical equivalence.
- not pyBuildingEnergy numerical equivalence.
- not EnergyPlus numerical equivalence.
- not ASHRAE Standard 140 benchmark-grade claim.

## Purpose

Step 01 created a deterministic three-node request for the existing `Iso52016MatrixHourlySolver`:

- air node;
- aggregated internal surface node;
- aggregated thermal mass node.

Step 02 keeps the same solver and adds an adapter path for explicit surfaces. Each declared surface can be expanded into:

- an internal surface node;
- a thermal mass node;
- air-to-surface conductance;
- surface-to-mass conductance;
- surface-to-boundary conductance.

## Surface/construction contracts

The stage introduces:

- `Iso52016PhysicalSurface`;
- `Iso52016PhysicalConstructionLayer`;
- `Iso52016PhysicalSurfaceBoundaryType`.

The builder estimates:

- construction conductance from `Area / sum(thickness / conductivity)`;
- construction heat capacity from `Area * sum(thickness * density * specific heat)`;
- default surface-node capacity as a fraction of construction capacity;
- default mass-node capacity as the remaining construction capacity;
- default surface-to-air conductance from area and a model option;
- default surface-to-mass conductance from boundary conductance and a model option.

These are deterministic engineering anchors for internal model-building behavior. They are not external numerical validation.

## Gains distribution

For explicit surfaces:

- convective internal gains are assigned to the air node;
- solar gains after optional air split are assigned to surface nodes;
- radiative internal gains are assigned to surface nodes;
- explicit distribution fractions must be provided for all surfaces or for none;
- if no explicit distribution fractions are provided, area weighting is used.

## Verification

Use:

```powershell
.\scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1
```

The verifier checks required files, claim-boundary language, the Step 01 dependency verifier, and deterministic tests.

## Not covered yet

This step does not add:

- per-surface hourly boundary temperature profiles;
- adjacent conditioned/unconditioned profile adapters;
- hourly operation schedules or ventilation overrides;
- external validation against EnergyPlus, ASHRAE 140, or pyBuildingEnergy outputs.