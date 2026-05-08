# AE-ISO52016-002 Step 13 - physical selection application integration hardening

This stage hardens the application-facing integration boundary around the ISO52016-inspired physical model selection layer.

## Scope

Step 13 does not add a new solver and does not add new heat-balance physics. It is an application integration guard stage for the existing selection layer introduced after the physical room model chain was built.

The guarded application rule is:

- `ReducedMatrix` remains the default application-facing path.
- `PhysicalNodeModel` is explicit opt-in.
- The selection layer uses the existing reduced Matrix builder, physical room model builder, and Matrix hourly solver.
- The stage must not silently replace the reduced Matrix path or weaken existing Matrix verification gates.

## Guarded files

The stage expects the selection contracts and service to remain discoverable under the ISO52016 physical contracts/services namespace:

- `Iso52016PhysicalModelSelectionStrategy`
- `Iso52016PhysicalModelSelectionRequest`
- `Iso52016PhysicalModelSelectionResult`
- `IIso52016PhysicalModelSelectionService`
- `Iso52016PhysicalModelSelectionService`

## Claim boundary

This is an ISO52016-inspired application integration hardening stage with validation/internal engineering anchors only.

It is not full ISO 52016 equivalence, not complete ISO 52016 numerical equivalence, not StandardReference equivalence, not EnergyPlus comparison workflow, and not ASHRAE Standard 140 validation.

No generated artifacts are introduced by this step.

## Step 13 verification literal markers

These literal markers are intentionally duplicated for Step 13 guard tests. They document the application-facing selection boundary without adding new calculation physics.
- ReducedMatrix remains the default
- PhysicalNodeModel is explicit opt-in
- existing reduced Matrix behavior is not replaced
- selection service uses existing builders and existing Matrix solver
- Not complete ISO 52016 numerical equivalence.
- Not complete ISO52016 numerical equivalence.
- Not full ISO 52016 equivalence.
- Not StandardReference numerical equivalence.
- Not EnergyPlus numerical equivalence.
- Not ASHRAE Standard 140 validation.
- Not ASHRAE Standard 140 benchmark-grade claim.

## Step 13 guard literals

ReducedMatrix remains the default.

PhysicalNodeModel is explicit opt-in.

This stage is validation/internal engineering anchors only.

This stage is not full ISO 52016 equivalence, not complete ISO 52016 numerical equivalence, not StandardReference equivalence, not EnergyPlus comparison workflow, not ASHRAE 140 / BESTEST-style validation anchor, not ASHRAE Standard 140 validation, and not ASHRAE Standard 140 benchmark-grade claim.

- Not ASHRAE 140 / BESTEST-style validation anchor.


## Step 13 guard literal repair

ReducedMatrix remains the default.
PhysicalNodeModel is explicit opt-in.
Physical model selection application integration hardening.
validation/internal engineering anchors only.
Not complete ISO 52016 numerical equivalence.
Not StandardReference numerical equivalence.
Not EnergyPlus numerical equivalence.
Not ASHRAE Standard 140 validation.
Not ASHRAE Standard 140 benchmark-grade claim.
