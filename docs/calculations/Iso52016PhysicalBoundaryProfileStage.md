# AE-ISO52016-002 Step 03 - physical boundary profile stage

This stage extends the ISO52016-inspired physical room/zone model builder with optional hourly boundary driving temperature overrides per physical surface.

## Scope

Step 03 keeps the existing Matrix solver and the Step 02 surface/construction expansion. It adds a small application-owned contract:

- `Iso52016PhysicalSurfaceHourlyBoundaryCondition`

The builder can now map per-surface, per-hour boundary driving temperatures into the Matrix request `BoundaryTemperaturesC` dictionary. When a surface has hourly boundary conditions and no explicit `BoundaryId`, the builder creates a deterministic surface-specific boundary id such as `outdoor:wall-east` or `ground:slab`. This avoids accidentally applying one overridden outdoor/ground/adjacent temperature to every surface that shares the same default boundary id.

## Behaviour

- Without `SurfaceBoundaryConditions`, Step 02 behaviour is preserved.
- With `SurfaceBoundaryConditions`, only the referenced surface/hour gets the provided driving temperature.
- Missing hours fall back to the normal boundary temperature source:
  - outdoor surfaces use hourly outdoor temperature;
  - ground surfaces use hourly ground boundary temperature;
  - adjacent conditioned/unconditioned surfaces use the surface adjacent override or the model default adjacent temperature.
- Ventilation boundary behaviour is unchanged.

## Validation anchors

The stage rejects:

- boundary conditions without explicit surfaces;
- unknown surface ids;
- duplicate `(surface id, hour of year)` entries;
- hours that do not exist in the hourly profile;
- non-finite boundary temperatures.

## Claim boundary

This is an ISO52016-inspired physical boundary profile stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.

