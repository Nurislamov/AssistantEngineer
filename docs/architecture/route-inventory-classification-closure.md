# Route Inventory Classification Closure (P8-05)

## Purpose

Close as much deferred/unknown route-inventory classification debt as safely possible without changing runtime/controller behavior.

## Scope

- Reclassify `UnknownNeedsClassification` inventory entries where route intent is clear.
- Tighten inventory coverage by moving safe routes from ignore-list to explicit inventory entries.
- Normalize tenant scope and operational categories for newly explicit entries.
- Keep deferred rollout entries explicit where authorization rollout is still staged by design.

## Non-claims

- No public API route change claim.
- No authorization behavior change claim.
- No full tenant isolation claim.
- No DB row-level security claim.
- No global EF query filter claim.
- No ownership backfill execution claim.
- No production security certification claim.

## Starting inventory state

Baseline carried from P7-06:

- `total=69`
- `deferred=21`
- `unknownGroup=3`
- `multi=22`

## Classification updates

- Reclassified prior `UnknownNeedsClassification` placeholders for:
  - `ThermalZonesController`
  - `RoomGroundContactController`
  - `RoomVentilationController`
- Added explicit inventory entries for previously ignored routes where classification was safe:
  - `EngineeringCoreStatusController` status/diagnostics routes.
  - `StandardProfilesController` room-usage route.
  - `StandardTablesController` catalog/reference routes.
  - `AnnualProfilesController` annual profile generation route.
  - `BuildingValidationController` auto-correct preview/apply routes.
  - `DevelopmentDemoDataController` demo seed route.
  - `DomesticHotWaterController` demand route.
  - `GroundTemperatureController` ground-temperature profile route.
  - `EngineeringWorkflowController` validation route.

## Deferred entries retained

Deferred entries remain for staged protection rollout groups where policy rollout is intentionally not complete (for example aggregate project/building/workflow/report route groups and admin/artifact-write deferred posture).

## Unknown entries retained

No `UnknownNeedsClassification` entries remain in endpoint-group/stage/tenant-scope fields after P8-05 closure updates.

## Ignore-list updates

Removed ignore entries now represented by explicit inventory routes:

- `AnnualProfilesController` route group ignore.
- `BuildingValidationController` route group ignore.
- `DevelopmentDemoDataController` route group ignore.
- `DomesticHotWaterController` route group ignore.
- `EngineeringCoreStatusController` route group ignore.
- `EngineeringWorkflowController` validate route ignore.
- `GroundTemperatureController` route group ignore.
- `StandardProfilesController` route group ignore.
- `StandardTablesController` route group ignore.

Remaining ignore entries stay for controllers that still require broad per-route staged split.

## Operational category updates

Newly explicit entries were normalized with explicit `rateLimitCategory` and `auditCategory` values aligned to classification model and existing route-governance checks.

## Tenant scope updates

Newly explicit entries were normalized to concrete tenant scope values where clear (`Building`, `Project`, or `NotApplicable`) instead of unknown placeholders.

## Remaining limitations

- Inventory still contains `Deferred` staged rollout entries by design.
- Some entries remain `MULTI` aggregates and should be split in later governance refinement.
- Route discovery remains text-level, not full runtime endpoint-graph inspection.

## Verification

- Route-governance suite remains green after classification updates:
  - `P7RouteInventoryCoverageTests`
  - `P7RouteClaimsConsistencyTests`
  - `P7RouteOperationalCategoryConsistencyTests`
  - `P7RouteTenantScopeConsistencyTests`
  - `P7ProtectionStageConsistencyTests`

## Next steps

- P8-06 scripts/tools rationalization.
