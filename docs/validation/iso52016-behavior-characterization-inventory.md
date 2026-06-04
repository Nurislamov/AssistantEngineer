# ISO52016 Behavior Characterization Inventory (P9-01A baseline, hardened in P9-01B1)

## Purpose

Freeze current ISO52016 seam behavior before any decomposition refactor so we can detect numerical or contract drift.

## Scope

This inventory covers ISO52016 component-level behavior locks, characterization test coverage, remaining seam gaps, and deterministic tolerance rules.

## Non-claims

- No calculation physics change claim.
- No expected value change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Characterization strategy

- Reuse deterministic existing fixtures and baseline anchor tests.
- Add focused seam-level tests only where setup is stable and non-brittle.
- Keep unresolved seam areas explicit in `gapsRetained` until P9-01B/P9-01C/P9-01D.
- Link retained gaps to `docs/validation/iso52016-matrix-solver-seam-design.json` for extraction-stage prerequisites.
- Link hardening evidence to `docs/validation/iso52016-matrix-solver-characterization-hardening.json`.

## Component coverage matrix

Canonical machine-readable matrix: `docs/validation/iso52016-behavior-characterization-inventory.json`.

Coverage levels used:

- `FocusedCharacterization`
- `BroadIntegrationCharacterization`
- `InternalInvariant`
- `GovernanceOnly`
- `Missing`

## Existing behavior-locking tests

- Matrix/solver baseline and anchor set:
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixBaselineFixtureTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixExternalValidationFixtureTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixExternalValidationAnnualAnchorTests.cs`
- Multi-zone coupling:
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/MultiZone/Iso52016MultiZoneHourlySolverTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/MultiZone/Iso52016MultiZoneFixtureTests.cs`
- Diagnostics/report mapping:
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Iso52016ResponseDiagnosticsVisibilityTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Iso52016AnnualDiagnosticsVisibilityTests.cs`

## New characterization tests

- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixAssemblyCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixAssemblyInvariantTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016LoadVectorCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016SolverOutputCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016SolverKernelCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016AggregationCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016ReportMappingCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016DiagnosticsResultMappingCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MultiZoneCouplingCharacterizationTests.cs`

## Gaps retained

- No direct intermediate coefficient-vector snapshot lock for matrix assembly internals (deferred to P9-01B design seam).
- Report-field-to-physics-anchor end-to-end traceability remains partial and staged for P9-02.
- Multi-zone normalization/validator absolute pinned coupling anchors remain partial and are staged for P9-01B6.

## Numerical tolerance policy

- Existing pinned fixtures retain their repository-declared tolerance.
- New seam tests use deterministic absolute tolerances:
  - `1e-6` for annual/hourly load and temperature assertions.
  - `1e-9` for deterministic repeat-run invariants on derived aggregates.

## Determinism policy

- No environment/timezone/random-dependent assertions.
- Repeat-run assertions use identical in-memory requests in one test process.
- No fixture expected-value edits are allowed in P9-01A.

## Refactor safety contract

- Future decomposition stages must keep:
  - numerical outputs within existing tolerances;
  - expected-value fixtures unchanged unless explicitly approved in a calculation-change stage;
  - no additional validation parity/certification claims.

## Next steps

- P9-01B: matrix assembly/solver seam extraction design.
- P9-01B1: matrix assembly/solver characterization hardening.
- P9-01C: report/diagnostics mapping seam review.
- P9-01D: weather/solar/gains and multi-zone input pipeline seam review.
