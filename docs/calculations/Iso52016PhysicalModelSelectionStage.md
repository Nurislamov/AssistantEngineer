# AE-ISO52016-002 Step 10 - physical model selection adapter stage

This stage adds an application-facing strategy selector over the already implemented Matrix paths.

## Scope

Step 10 keeps the existing Matrix solver and the existing reduced Matrix behaviour. It adds a small C# adapter layer:

- `Iso52016PhysicalModelSelectionStrategy`;
- `Iso52016PhysicalModelSelectionRequest`;
- `Iso52016PhysicalModelSelectionResult`;
- `ISo52016PhysicalModelSelectionService`;
- `Iso52016PhysicalModelSelectionService`.

The service selects between:

- `ReducedMatrix` - the existing reduced Matrix request builder and existing Matrix solver;
- `PhysicalNodeModel` - the ISO52016-inspired physical room model builder and existing Matrix solver.

Reduced Matrix remains the default application path. The physical node model requires explicit strategy selection.

## Behaviour

The adapter accepts an already prepared `Iso52016RoomHourlyInputProfile`. This avoids duplicating weather, solar and internal-gain pipelines and keeps this stage focused on model selection.

The adapter returns the selected strategy, generated Matrix request and Matrix solver profile together. This preserves auditability and allows diagnostics to inspect the generated topology.

## Validation anchors

The stage rejects:

- missing model selection requests;
- missing hourly input profiles;
- unsupported strategy enum values;
- invalid physical model inputs through propagated builder validation failures;
- invalid Matrix solve results through propagated solver failures.

## Claim boundary

This is an ISO52016-inspired physical model selection adapter stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not StandardReference numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.