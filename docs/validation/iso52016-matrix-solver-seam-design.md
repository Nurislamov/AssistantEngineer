# ISO52016 Matrix/Solver Seam Design (P9-01B)

## Purpose

Define behavior-preserving decomposition seams for ISO52016 matrix assembly and solver internals before any extraction refactor.

## Scope

This stage is design-only and documents seam boundaries, invariants, risk controls, and test prerequisites for future extraction work.

## Non-claims

- No calculation physics change claim.
- No expected value change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Current matrix/solver responsibilities

- Matrix input preparation spans:
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixRoomEnergySimulationService.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016RoomEnergySimulationService.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/MultiZone/Iso52016MultiZoneNormalizedInputBuilder.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/MultiZone/Iso52016MultiZoneInputValidator.cs`
- Matrix assembly and solve are concentrated in:
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixHourlySolver.cs`
- Result mapping and diagnostics include:
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixRoomEnergySimulationResultMapper.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016HourlyHeatBalanceDiagnosticsBuilder.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/MultiZone/Iso52016MultiZoneSolverDiagnostics.cs`

## Proposed seam model

- Matrix input preparation seam:
  - Proposed target: `IISO52016MatrixInputPreparer`
  - Role: isolate normalization + pre-solver profile/model preparation.
- Matrix assembly seam:
  - Proposed target: `IISO52016MatrixAssembler`
  - Role: isolate coefficient matrix creation and boundary-conductance shaping.
- Load vector seam:
  - Proposed target: `IISO52016LoadVectorAssembler`
  - Role: isolate right-hand side term construction (capacity carry-over, gains, HVAC load, boundary temperature terms).
- Solver kernel seam:
  - Proposed target: `IISO52016MatrixSolverKernel`
  - Role: isolate linear solve kernel and singular/ill-conditioned handling.
- Hourly result mapping seam:
  - Proposed target: `IISO52016HourlyResultMapper`
  - Role: isolate node-state/hourly result projection and monthly summary aggregation mapping.
- Diagnostics seam:
  - Proposed target: `IISO52016SolverDiagnosticsBuilder`
  - Role: isolate diagnostics shaping for report-facing and governance-facing output.
- Multi-zone coupling seam:
  - Proposed target: `IISO52016MultiZoneCouplingAssembler`
  - Role: isolate boundary-link/coupling preparation and pair deduplication policy.

## Matrix input preparation seam

Primary boundary:
- request/profile/model preparation without matrix coefficient or solve logic.

Invariants:
- same accepted profile lengths and validation outcomes;
- same defaulting and deterministic profile-expansion behavior;
- no changes to claim-flag boundary handling.

## Matrix assembly seam

Primary boundary:
- coefficient matrix assembly from nodes, conductance links, boundary conductance, and time-step settings.

Invariants:
- matrix dimensions and node ordering remain stable for same input;
- diagonal/off-diagonal sign conventions remain unchanged;
- no tolerance broadening in coefficient-related characterization checks.

## Solver kernel seam

Primary boundary:
- pivoting/solve execution and singular/ill-conditioned detection.

Invariants:
- deterministic repeated-run outputs for identical input;
- no NaN/Infinity in solved temperatures;
- same failure behavior when matrix is singular/ill-conditioned.

## Result mapping seam

Primary boundary:
- map solved node temperatures and HVAC loads into hourly and monthly output contracts.

Invariants:
- hourly and monthly summaries remain numerically consistent with existing characterization tests;
- no output-field semantic drift in report-facing mapping.

## Diagnostics seam

Primary boundary:
- diagnostics projection and disclosure-safe mapping.

Invariants:
- existing diagnostics/report characterization remains unchanged;
- no unsupported parity/certification wording in diagnostics metadata.

## Multi-zone coupling seam

Primary boundary:
- inter-zone pair deduplication and adjacent-boundary coupling assembly.

Invariants:
- no duplicate-pair double counting regression;
- same same-use adiabatic policy behavior for current options;
- same zone-link validity constraints.

## Characterization coverage per seam

Current coverage sources:
- `docs/validation/iso52016-behavior-characterization-inventory.json`
- `docs/validation/iso52016-matrix-solver-characterization-hardening.json`
- focused tests:
  - `Iso52016MatrixAssemblyCharacterizationTests`
  - `Iso52016SolverOutputCharacterizationTests`
  - `Iso52016AggregationCharacterizationTests`
  - `Iso52016ReportMappingCharacterizationTests`
- existing service/integration tests in matrix and multi-zone suites.

Retained pre-extraction gaps:
- internal coefficient/vector intermediate snapshot locks remain intentionally limited to avoid brittle coupling;
- multi-zone validator/builder sub-seam coverage is still broader than focused seam locking;
- absolute pinned coupled-output anchors for multi-zone seam remain staged for `P9-01B6`.

## Invariants to preserve

- no formula edits;
- no expected-value fixture edits;
- no tolerance broadening beyond current characterization policy;
- deterministic repeated-run behavior for same inputs;
- no NaN/Infinity in solver results;
- no change to public API/DTO/workflow/auth behavior through these internal seams.

## Refactor sequence proposal

- `P9-01B1` Matrix assembly seam characterization hardening (`TestOnly`, implemented)
- `P9-01B2` Extract matrix input preparation seam (`RefactorOnly`)
- `P9-01B3` Extract matrix coefficient/vector assembly seam (`RefactorOnly`)
- `P9-01B4` Extract solver kernel wrapper (`RefactorOnly`)
- `P9-01B5` Extract result/diagnostics mapper seam (`RefactorOnly`)
- `P9-01B6` Multi-zone coupling seam review (`AuditOnly` first, optional `RefactorOnly` later)

Each stage must keep behavior-change permission disabled and pass unchanged characterization guards.

## Risks and mitigations

Canonical risk register:
- `docs/validation/iso52016-matrix-solver-seam-risk-register.md`
- `docs/validation/iso52016-matrix-solver-seam-risk-register.json`

Hardening evidence:
- `docs/validation/iso52016-matrix-solver-characterization-hardening.md`
- `docs/validation/iso52016-matrix-solver-characterization-hardening.json`

## Deferred items

- Deeper internal coefficient-term provenance trace map (planned follow-up after B1 characterization hardening).
- Full report-field-to-physics-anchor traceability remains aligned with `P9-02`.
- Promotion of planned placeholder external comparisons remains aligned with `P9-08`.

## Next steps

- Use the `P9-01B1` hardening baseline as extraction precondition for `P9-01B2..P9-01B6`.
- Keep decomposition review/component map/behavior inventory synchronized with seam-stage progress.
