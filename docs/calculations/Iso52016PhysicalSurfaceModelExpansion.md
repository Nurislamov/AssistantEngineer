# AE-ISO52016-002 Step 02 - physical surface and construction expansion

## Status

This stage expands the ISO52016-inspired physical node model builder introduced in Step 01.

The claim boundary remains intentionally narrow:

- ISO52016-inspired physical surface/construction expansion stage;
- validation/internal engineering anchors only;
- not complete ISO 52016 numerical equivalence;
- not pyBuildingEnergy numerical equivalence;
- not EnergyPlus numerical equivalence;
- not ASHRAE Standard 140 benchmark-grade claim.

## Purpose

Step 01 introduced a deterministic three-node room model over the existing `Iso52016MatrixHourlySolver`:

- air;
- internal surface;
- thermal mass.

Step 02 adds explicit physical surfaces and construction layers so the application-side model can start representing a room as multiple surface and mass nodes without writing a new solver.

## New contracts

Step 02 adds:

| Contract | Purpose |
| --- | --- |
| `Iso52016PhysicalSurfaceBoundaryType` | Outdoor, ground, adjacent conditioned and adjacent unconditioned boundary classification. |
| `Iso52016PhysicalConstructionLayer` | Thickness, conductivity, density and specific heat for one construction layer. |
| `Iso52016PhysicalSurface` | Area, boundary type, construction layers, optional node ids, conductance overrides and gain-distribution fractions. |

`Iso52016PhysicalRoomModelRequest` now accepts an optional `Surfaces` collection.

## Builder behavior

The builder remains additive and preserves Step 01 behavior:

- if `Surfaces` is empty, the deterministic three-node fallback is used;
- if `Surfaces` is provided, the builder creates:
  - one air node;
  - one surface node per physical surface;
  - one mass node per physical surface.

For each physical surface the builder adds:

- air РІвЂ вЂќ surface conductance;
- surface РІвЂ вЂќ surface-mass conductance;
- surface РІвЂ вЂќ boundary conductance.

The surface boundary may be outdoor, ground, adjacent conditioned, or adjacent unconditioned.

## Construction-derived anchors

For Step 02 deterministic anchors:

- layer thermal resistance is `thickness / conductivity`;
- surface boundary conductance is `area / sum(layer resistances)` unless explicitly overridden;
- construction heat capacity is `area * sum(thickness * density * specific heat)`;
- surface node and mass node heat capacity are split by explicit options unless explicitly overridden.

These are internal engineering anchors only. They are not a full ISO 52016 construction transfer-function or finite-difference implementation.

## Gains distribution

Internal gains keep the Step 01 convective/radiative split:

- convective internal gains go to the air node;
- radiative internal gains are distributed over surface nodes.

Solar gains are also distributed over surface nodes.

Distribution is deterministic:

- if every surface provides configured fractions, those fractions must sum to 1.0;
- otherwise the builder uses surface-area weighting.

## Verification

Primary stage verification:

```powershell
.\scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1
```

Fast structure-only check:

```powershell
.\scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1 -SkipTests
```

All Matrix verification chain after this step:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

This step does not create generated artifacts. Generated validation outputs, if added later, must remain under ignored artifact folders unless intentionally promoted as source fixtures.