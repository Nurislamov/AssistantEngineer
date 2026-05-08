# AE-ISO52016-002 Step 05 - physical room simulation service adapter

This stage adds an application-owned service adapter over the ISO52016-inspired physical room node model builder and the existing Matrix hourly solver.

## Scope

Step 05 keeps the existing Matrix solver and all earlier physical node model builder stages. It adds:

- `IIso52016PhysicalRoomEnergySimulationService`;
- `Iso52016PhysicalRoomEnergySimulationService`;
- `Iso52016PhysicalRoomEnergySimulationResult`.

The service does not introduce a new solver. It performs the following deterministic pipeline:

1. validate and build the physical room model into an `Iso52016MatrixHourlySolverRequest`;
2. run the existing `IIso52016MatrixHourlySolver`;
3. return the physical request, generated Matrix request and Matrix solver profile together for engineering traceability.

## Why this stage exists

Before this step, the physical builder could create a Matrix request, but callers still had to wire the builder and solver manually. Step 05 makes the physical path explicit and testable without moving formula logic into PowerShell scripts.

PowerShell remains a thin developer/CI entrypoint. Calculation logic and verification rules that matter for behaviour remain in C# tests and services.

## Behaviour covered by internal anchors

The deterministic tests cover:

- successful simulation through the physical builder and existing Matrix solver;
- preservation of hourly operation profile ventilation overrides in the generated Matrix request;
- propagation of physical builder validation errors;
- null request rejection;
- registration traceability.

## Claim boundary

This is an ISO52016-inspired physical room simulation service adapter stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not StandardReference numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.