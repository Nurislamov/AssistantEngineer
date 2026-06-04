# ISO52016 Matrix/Solver Characterization Hardening (P9-01B1)

## Purpose

Strengthen focused characterization guardrails for matrix assembly/solver seams before any extraction refactor.

## Scope

This stage is `TestOnly`: it adds/expands characterization coverage for matrix/vector/kernel/coupling/diagnostics seams and updates governance artifacts. No production solver behavior changes are in scope.

## Non-claims

- No calculation physics change claim.
- No expected value change claim.
- No fixture numeric value change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Added characterization tests

- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixAssemblyInvariantTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016LoadVectorCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016SolverKernelCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MultiZoneCouplingCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016DiagnosticsResultMappingCharacterizationTests.cs`

## Seams covered

- Matrix assembly seam (`P9-01B-SEAM-002`)
- Load vector seam (`P9-01B-SEAM-003`)
- Solver kernel seam (`P9-01B-SEAM-004`)
- Multi-zone coupling seam (`P9-01B-SEAM-007`)
- Result/diagnostics mapping seam (`P9-01B-SEAM-005`, `P9-01B-SEAM-006`)

## Invariants pinned

- Matrix dimension equals node-count for deterministic requests.
- Coefficients and vector entries are finite (no NaN/Infinity).
- Diagonal/off-diagonal sign conventions remain stable for characterized cases.
- RHS gain/HVAC injection behavior remains stable for characterized cases.
- Solver-kernel output remains deterministic for repeated runs.
- Singular matrix behavior remains explicit and stable.
- Multi-zone coupled run remains deterministic and finite for characterized fixture.
- Report-facing mapped fields remain finite and deterministic for selected diagnostics.

## Tolerance policy

P9-01A policy is preserved without widening:

- `1e-6` for pinned annual/hourly numeric assertions.
- `1e-9` for repeated-run deterministic invariants.
- NaN/Infinity are forbidden.

## Retained gaps

- Intermediate full coefficient-term snapshot locking remains intentionally limited to avoid brittle coupling (`P9-01A-GAP-001`).
- Full anchor-to-report traceability is still staged for `P9-02` (`P9-01A-GAP-002`).
- Multi-zone absolute pinned coupled-output anchors remain staged for `P9-01B6`; current stage adds deterministic/finite/shape locks (`P9-01A-GAP-003`).

## Expected value policy

- No existing expected fixture values were changed.
- No new fixture files replace or overwrite existing baselines.
- New pinned assertions characterize current behavior only and do not claim formal validation.

## Refactor safety contract

- Any P9-01B2..B6 extraction must keep `behaviorChangeAllowed=false`.
- Required pre-extraction suites must stay green.
- Tolerance policy widening remains disallowed in this stage.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`
- ownership backfill apply-disabled boundary check (must remain disabled and secret-safe)

## Next steps

- Proceed to `P9-01B2` only after confirming retained gaps are acceptable for the targeted seam.
- If stronger coupling anchors are required first, execute `P9-01B6` characterization refinement before extraction.
