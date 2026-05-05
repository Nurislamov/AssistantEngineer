# AE-ISO52016-002 Step 01 - ISO52016-inspired physical node model builder

## Status

This stage introduces the first physical room/zone node-model builder on top of the existing `Iso52016MatrixHourlySolver`.

The claim boundary is intentionally narrow:

- ISO52016-inspired physical node model stage;
- validation/internal engineering anchors only;
- not complete ISO 52016 numerical equivalence;
- not pyBuildingEnergy numerical equivalence;
- not EnergyPlus numerical equivalence;
- not ASHRAE Standard 140 benchmark-grade claim.

## Purpose

The previous Matrix solver stage proved that the solver can handle nodes, conductance links, hourly boundary temperatures, node gains and setpoint-limited heating/cooling energy.

The remaining gap is the application-side model mapping. Step 01 closes the first part of that gap by adding a deterministic builder that maps an existing `Iso52016RoomHourlyInputProfile` into a multi-node Matrix request.

## Physical nodes introduced

The initial model is deliberately compact and deterministic:

| Node | Matrix role |
| --- | --- |
| `air` | controlled air node used by the Matrix solver setpoint logic |
| `internal-surface` | internal radiant/surface exchange node |
| `thermal-mass` | lumped thermal mass storage node |

The room thermal capacity is split by options into air, internal surface and thermal mass fractions. Defaults are guarded by tests and sum to 1.0.

## Conductance links introduced

Internal links:

- air Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў internal surface;
- internal surface Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў thermal mass.

Boundary links:

- internal surface Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў outdoor transmission boundary;
- internal surface Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў ground boundary;
- internal surface Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў adjacent zone boundary;
- air Р В Р’В Р вЂ™Р’В Р В Р’В Р Р†Р вЂљР’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В РІР‚в„ўР вЂ™Р’В Р В Р’В Р В РІР‚В Р В Р’В Р Р†Р вЂљРЎв„ўР В Р Р‹Р РЋРЎв„ў ventilation/infiltration boundary.

The transmission conductance is split across outdoor, ground and adjacent boundaries by deterministic fractions. Ventilation/infiltration is mapped to the air node and uses outdoor air temperature as the boundary temperature in this first stage.

## Gains distribution

Internal gains are split into:

- convective gains to the air node;
- radiative gains distributed to internal surface and thermal mass nodes.

Solar gains are distributed to:

- internal surface node;
- thermal mass node.

All fractions are explicit options and are guarded by deterministic unit tests.

## What this stage does not do yet

This step does not introduce:

- a full ISO 52016 physical layer-by-layer construction model;
- time-varying conductance links;
- a full adjacent conditioned/unconditioned zone coupling model;
- not pyBuildingEnergy numerical equivalence;
- no EnergyPlus numerical equivalence;
- not ASHRAE Standard 140 benchmark-grade claim.

Those remain future work and require external numerical validation before any stronger claims can be made.

## Verification

Primary stage verification:

```powershell
.\scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1
```

Fast structure-only check:

```powershell
.\scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1 -SkipTests
```

All Matrix verification chain after this step:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

This step does not create generated artifacts. If future physical-node baseline artifacts are added, they must be placed under ignored artifact folders and not committed unless explicitly intended as source fixtures.
## AE-ISO52016-002 Step 02 - surface and construction expansion

Step 02 extends the Step 01 physical node builder with optional explicit physical surfaces and construction layers.

When `Iso52016PhysicalRoomModelRequest.Surfaces` is empty, the deterministic Step 01 three-node fallback remains active. When surfaces are provided, the builder creates one surface node and one mass node per physical surface, plus conductance links:

- air node Р В Р’В Р В РІР‚В Р В Р вЂ Р В РІР‚С™Р вЂ™Р’В Р В Р вЂ Р В РІР‚С™Р РЋРЎС™ surface node;
- surface node Р В Р’В Р В РІР‚В Р В Р вЂ Р В РІР‚С™Р вЂ™Р’В Р В Р вЂ Р В РІР‚С™Р РЋРЎС™ surface mass node;
- surface node Р В Р’В Р В РІР‚В Р В Р вЂ Р В РІР‚С™Р вЂ™Р’В Р В Р вЂ Р В РІР‚С™Р РЋРЎС™ outdoor, ground, adjacent conditioned, or adjacent unconditioned boundary.

Solar and internal radiative gains are distributed either by configured fractions or by deterministic surface-area weighting. This is still an ISO52016-inspired internal engineering stage only, not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.
## AE-ISO52016-002 Step 03 - physical boundary profiles

Step 03 extends the explicit surface/construction model with optional per-surface hourly boundary driving temperatures.

The builder keeps Step 02 behaviour when no `SurfaceBoundaryConditions` are provided. When boundary conditions are provided for a surface without an explicit boundary id, the builder creates deterministic surface-specific boundary ids such as `outdoor:wall-east` so that one overridden driving temperature does not leak to other surfaces sharing the default outdoor/ground/adjacent boundary id.

This remains an ISO52016-inspired internal engineering stage only, not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.
## AE-ISO52016-002 Step 04 - physical operation profiles

Step 04 extends the physical room/zone builder with optional hourly operation profiles for ventilation/infiltration boundary conductance, ventilation boundary temperature, internal gains convective split, and direct solar-to-air split.

The stage also extends the existing Matrix hourly input contract with optional hourly boundary conductance overrides. This avoids pretending that variable ventilation can be represented by a static conductance. The solver remains the same implicit Matrix solver; Step 04 only adds an optional per-hour conductance input path used by the physical builder.

This remains an ISO52016-inspired internal engineering stage only, not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.




