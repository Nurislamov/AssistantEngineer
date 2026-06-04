# ISO52016 Component Map (P9-01 / P9-01A / P9-01B / P9-01B1)

## Purpose

Provide a stable component-to-file/test/evidence map for the ISO52016 calculation surface to support staged decomposition planning without changing physics or expected values.

## Scope

This map covers ISO52016 calculation services in `AssistantEngineer.Modules.Calculations`, associated verification tooling/scripts, and linked validation evidence/provenance entries.

## Non-claims

- No calculation physics change claim.
- No expected numerical values change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Component map summary

- Input normalization/adaptation
- Weather/solar input preparation
- Internal gains preparation
- Boundary/thermal element modeling
- Zone model building
- Matrix assembly and solving
- Multi-zone solving and coupling
- Hourly and annual aggregation
- Report/diagnostics mapping
- Validation fixtures/tooling
- Workflow integration

Detailed machine-readable map:

- `docs/validation/iso52016-component-map.json`
- `docs/validation/iso52016-behavior-characterization-inventory.json`
- `docs/validation/iso52016-matrix-solver-seam-design.json`
- `docs/validation/iso52016-matrix-solver-characterization-hardening.json`

## Maturity linkage

- ISO52016 maturity from P9-00 remains `IndependentReferenceFixture`.
- Report/workflow integration maturity remains `InternalInvariant`.
- External registry and fixture tooling references remain bounded by `ExternalToolReferenceFixture` and `PlannedPlaceholder` rules from P9-03 provenance inventory.

## Provenance linkage

- Evidence inventory: `docs/validation/validation-evidence-inventory.json`
- Provenance inventory: `docs/validation/validation-fixture-provenance-inventory.json`
- Behavior characterization inventory: `docs/validation/iso52016-behavior-characterization-inventory.json`
- Seam design and risk register:
  - `docs/validation/iso52016-matrix-solver-seam-design.json`
  - `docs/validation/iso52016-matrix-solver-seam-risk-register.json`
  - `docs/validation/iso52016-matrix-solver-characterization-hardening.json`

## Known limitations

- Component seams are mapped for planning; no refactor extraction is executed in P9-01.
- Some behavior-locking tests remain broad integration tests and do not yet isolate every seam candidate.
- Planned placeholder external evidence remains excluded from achieved-evidence interpretation.
