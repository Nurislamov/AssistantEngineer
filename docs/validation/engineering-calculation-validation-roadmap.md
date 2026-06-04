# Engineering Calculation Validation Roadmap (P9-00, refreshed in P9-03/P9-01/P9-01A/P9-01B/P9-01B1)

## Purpose

Refresh the engineering-calculation validation roadmap for P9 with explicit maturity levels, evidence anchors, and claim boundaries.

## Scope

This roadmap covers:

- ISO52010 solar/weather chain;
- ISO52016 matrix solver chain;
- heating/cooling load calculations;
- report/workflow calculation outputs;
- external/reference evidence planning.

## Non-claims

- No calculation physics change claim.
- No pyBuildingEnergy full parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.
- No production security certification claim.
- No ownership backfill execution claim.
- No full tenant isolation claim.

## Current validation coverage summary

- ISO52010/solar-weather behavior has deterministic unit and integration coverage with solar/weather profile builders, Perez diagnostics, and shading/window fixtures.
- ISO52016 matrix has deterministic anchors, manual-independent references, and annual 8760 fixture coverage in staged external-validation directories.
- Heating/cooling load has manual engineering fixtures and invariant-style regression tests for room/floor/building aggregation paths.
- Workflow/report output paths have API characterization and contract tests for trace/report/export/artifact surfaces.
- External/reference evidence has fixture registries and harness tooling, but remains explicitly non-parity and non-certification.

## Maturity model

- `None`: no meaningful tests or documented evidence.
- `SmokeOnly`: basic smoke checks only.
- `InternalInvariant`: deterministic internal checks for structure/invariants.
- `ManualReferenceAnchor`: hand-derived engineering anchors with traceable fixture inputs.
- `IndependentReferenceFixture`: independent reference fixtures with explicit tolerances/provenance.
- `ExternalToolReferenceFixture`: external-tool-oriented fixture scaffolds/harnesses without parity/certification claims.
- `CrossImplementationComparison`: structured cross-implementation comparisons with bounded interpretation.
- `CandidateForFormalValidation`: governance candidate for future formal validation program entry.

## ISO52010 / solar-weather validation state

- Current maturity: `IndependentReferenceFixture`.
- Evidence highlights:
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Solar/SolarPositionCalculator.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Solar/PerezAnisotropicSurfaceIrradianceCalculator.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52010/Iso52010SolarChainCompletenessEvidenceTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/WeatherSolar/AnnualWeatherSolarProfileBuilderTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Solar/WindowShadingBenchmarkFixtureTests.cs`
- Known limitations:
  - external benchmark parity is not claimed;
  - certification-style validation is not claimed.

## ISO52016 matrix validation state

- Current maturity: `IndependentReferenceFixture`.
- Evidence highlights:
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnnualAnchors/`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationNamingAnchors/`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixExternalValidationFixtureTests.cs`
  - `tools/AssistantEngineer.Tools.Iso52016Verification/`
- Known limitations:
  - no full donor/reference parity claim;
  - no formal standard-case validation completion claim.

## Heating/cooling load validation state

- Current maturity: `ManualReferenceAnchor`.
- Evidence highlights:
  - `tests/AssistantEngineer.Tests/Calculations/HeatingLoadValidationTests.cs`
  - `tests/AssistantEngineer.Tests/Validation/ManualEngineering/`
  - `tests/AssistantEngineer.Tests/Validation/ExternalReferenceValidation/Fixtures/`
  - `docs/validation/manual-engineering-fixtures.md`
- Known limitations:
  - coverage is strong for anchor scenarios, not exhaustive for all production permutations;
  - no certification claim.

## Reports/workflow validation state

- Current maturity: `InternalInvariant`.
- Evidence highlights:
  - `tests/AssistantEngineer.Tests/Api/EngineeringWorkflow/EngineeringWorkflowControllerCharacterizationTests.cs`
  - `tests/AssistantEngineer.Tests/Api/EngineeringWorkflow/EngineeringWorkflowControllerResponseShapeTests.cs`
  - `tests/AssistantEngineer.Tests/Validation/ExternalReferenceValidation/FormulaAudit/EngineeringCoreV1ReportContractSnapshotTests.cs`
  - `tests/AssistantEngineer.Tests/Validation/ExternalReferenceValidation/FormulaAudit/EngineeringCoreV1ReportExportDisclosureGuardTests.cs`
- Known limitations:
  - characterization coverage freezes behavior/contracts but is not external scientific validation.

## External/reference evidence state

- Current maturity: `ExternalToolReferenceFixture`.
- Evidence highlights:
  - `docs/validation/EnergyPlusValidationCaseRegistry.json`
  - `docs/validation/EnergyPlusValidationFixtureCatalog.json`
  - `docs/validation/ExternalComparisonCaseRegistry.json`
  - `tools/AssistantEngineer.Tools.EnergyPlusValidation/`
  - `tools/AssistantEngineer.Tools.EnergyPlusFixtureAuthoring/`
  - `docs/validation/external-numerical-validation-roadmap.md`
- Known limitations:
  - fixture/harness evidence exists, but no parity/certification claim is allowed.

## Validation claim boundary

- `validation anchor` is not formal validation.
- `reference-informed` is not full donor/reference parity.
- `EnergyPlus-style fixture` is not EnergyPlus parity.
- `manual engineering fixture` is not certification.
- `ISO target implementation` is not ISO certification.

Canonical wording reference:
- `docs/architecture/terminology-and-claims-vocabulary.md`

## Gaps and risks

- Provenance metadata quality is uneven across older external-validation fixtures.
- Some external comparison registries remain placeholder-oriented with planned/reference-only outputs.
- Route/report characterization is strong for contract safety, but external evidence mapping for outputs is still incomplete.
- Release-ready validation outputs are distributed across multiple docs/tools and need a tighter cross-index map.

### P9-01 relationship

- P9-01 completed an audit-only ISO52016 decomposition review and component map:
  - `docs/validation/iso52016-decomposition-review.md`
  - `docs/validation/iso52016-component-map.md`
- P9-01 did not change formulas, expected values, fixtures, runtime behavior, or API contracts.

### P9-01A relationship

- P9-01A implemented a test/audit behavior characterization inventory before decomposition:
  - `docs/validation/iso52016-behavior-characterization-inventory.md`
  - focused seam tests for matrix assembly/solver/aggregation/report mapping.
- P9-01A did not change formulas, expected values, fixture numeric values, runtime behavior, or API contracts.

### P9-01B relationship

- P9-01B implemented design-only matrix/solver seam boundaries and risk register artifacts:
  - `docs/validation/iso52016-matrix-solver-seam-design.md`
  - `docs/validation/iso52016-matrix-solver-seam-risk-register.md`
- P9-01B did not change formulas, expected values, fixture numeric values, runtime behavior, or API contracts.

### P9-01B1 relationship

- P9-01B1 implemented test-only matrix/solver characterization hardening:
  - `docs/validation/iso52016-matrix-solver-characterization-hardening.md`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixAssemblyInvariantTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016LoadVectorCharacterizationTests.cs`
  - `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016SolverKernelCharacterizationTests.cs`
- P9-01B1 did not change formulas, expected values, fixture numeric values, runtime behavior, or API contracts.

### Gap status refresh (P9-03)

- `P9-GAP-001`: `PartiallyAddressed` in `P9-03` via provenance model + provenance inventory + planned-placeholder classification.
- `P9-GAP-002`: `Open` and targeted for `P9-02`.
- `P9-GAP-003`: `Open` and targeted for `P9-08`.
- `P9-GAP-004`: `Addressed` in `P9-00` by initial evidence inventory, reinforced in `P9-03` with provenance metadata.

## Recommended P9 backlog

- P9-01A: ISO52016 behavior characterization inventory (implemented as test-only seam/output freeze before decomposition).
- P9-01B: Matrix assembly/solver seam extraction design (implemented, design-only, no formula change).
- P9-01B1: Matrix assembly/solver characterization hardening (implemented, test-only, no formula/expected-value change).
- P9-01C: Report/diagnostics mapping seam review (audit/test only).
- P9-01D: Weather/solar/gains input pipeline seam review (audit/test only).
- P9-01E: ISO52016 naming/ubiquitous-language cleanup candidates (docs/refactor candidates with approval).
- P9-01F: Fixture traceability to ISO52016 components (docs/metadata only).
- P9-02: Heating/cooling load report builder consolidation (behavior-preserving refactor prep).
- P9-03: Validation fixture provenance cleanup (metadata and traceability quality).
- P9-04: Route inventory deferred items phase 2 (remaining governance closure).
- P9-08: External validation evidence planning (clear candidate paths and evidence promotion gates).

## Verification and release gate relationship

- Roadmap refresh is governance/audit only and does not alter runtime formulas.
- Existing release-ready gates remain the enforcement surface for current no-change constraints.
- Evidence planning from this roadmap is input to future P9 stages, not a release-boundary change.
- Provenance references for P9-03:
  - `docs/validation/validation-fixture-provenance-model.md`
  - `docs/validation/validation-fixture-provenance-inventory.md`

## Next steps

- Keep claim boundaries explicit in every new validation artifact.
- Promote only evidence-backed maturity upgrades in future P9 stages.
- Defer any calculation-physics change proposals to explicit approval stages.
