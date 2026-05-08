# AE-ISO52016-002 Step 11 - physical model selection application guard

This stage locks down the application-facing selection boundary for the ISO52016-inspired physical model chain.

## Scope

Step 10 introduced an explicit model selection adapter. Step 11 adds guard coverage around that adapter so the application path remains predictable:

- the reduced Matrix path remains the default;
- the physical node model path is explicit opt-in;
- the selection service delegates to existing builders and the existing Matrix solver;
- no new solver is introduced;
- no existing reduced/baseline stages are removed or replaced.

## Guarded behaviour

The guard tests inspect the selection contracts and service source to ensure:

- `ReducedMatrix` and `PhysicalNodeModel` remain visible strategy names;
- `Iso52016PhysicalModelSelectionRequest` defaults to `ReducedMatrix`;
- `Iso52016PhysicalModelSelectionService` depends on:
  - `IIso52016MatrixReducedRoomModelBuilder`;
  - `IIso52016PhysicalRoomModelBuilder`;
  - `IIso52016MatrixHourlySolver`;
- the service does not instantiate a new Matrix solver directly;
- the Matrix all-verification script exposes this stage for discoverability.

## Why this stage exists

The physical chain is now usable by an application-facing adapter. That creates a different risk than numerical solver errors: an accidental default switch from reduced Matrix to physical node model would be a breaking behavioural change. This stage makes that boundary explicit and guarded.

## Claim boundary

This is an ISO52016-inspired physical model selection application guard with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not StandardReference numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.
