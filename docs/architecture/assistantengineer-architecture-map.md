# AssistantEngineer Architecture Map (P9-01B1)

## Purpose

Provide a stable module/layer map of AssistantEngineer after the P5/P6/P7/P8 hardening cycle and P9 validation-roadmap/provenance governance refresh.

## Backend module map

- `AssistantEngineer.Api` (host/controllers/security boundary)
- `AssistantEngineer.Infrastructure` (EF, persistence integration, module wiring)
- `AssistantEngineer.Modules.Benchmarks`
- `AssistantEngineer.Modules.Buildings`
- `AssistantEngineer.Modules.Calculations`
- `AssistantEngineer.Modules.EngineeringWorkflow`
- `AssistantEngineer.Modules.Equipment`
- `AssistantEngineer.Modules.Identity`
- `AssistantEngineer.Modules.Reporting`
- `AssistantEngineer.SharedKernel`

## Domain modules

- Buildings domain entities and policies.
- Calculations domain/value models and solvers.
- Equipment domain catalogs and selection.
- Identity domain access policy primitives.
- Reporting domain contracts/builders.

## Application services

- Calculation orchestration and scenario/job services.
- Workflow preview/state/report orchestration services.
- Tenant-scoped read services and authorization guards.
- Reporting assembly/service composition.

## Infrastructure services

- Runtime DbContexts and migrations.
- Repository/adapters for module persistence.
- Configuration/options binding and external IO integrations.

## API controllers

- Projects/Buildings CRUD and protected read/write rollouts.
- Calculation/workflow/report endpoints.
- Reference-data, diagnostics, and compatibility endpoints.

## Tools

- OwnershipBackfill governance tooling.
- EngineeringCore release/evidence/contracts tooling.
- Iso52016 and EnergyPlus verification tooling.
- EngineeringGovernance and performance benchmark tooling.
- Repository boundary/hygiene verification tooling.

## Scripts

- `scripts/engineering-core` wrapper scripts around engineering tools.
- `scripts/iso52016` wrapper scripts around iso52016 verification tooling.

## Tests

- `tests/AssistantEngineer.Tests/Architecture` boundary/governance suite.
- `tests/AssistantEngineer.Tests/Api` runtime API/behavior/security tests.
- `tests/AssistantEngineer.Tests/Tools` tooling and CLI safety tests.

## Docs/governance

- `docs/security` for release boundary, guardrails, inventories, and staged governance evidence.
- `docs/adr` for architecture/security decisions and deferred-decision backlog.
- `docs/architecture` for hardening audits and technical-debt maps.

## Known boundaries

- Runtime API does not reference ownership backfill apply execution path.
- Ownership backfill CLI apply remains disabled boundary.
- Calculation physics remain in C# runtime modules, not in shell scripts.
- Security governance tooling and release scripts are operational wrappers, not runtime dependencies.
- EngineeringWorkflow module application contracts/services now use `AssistantEngineer.Modules.EngineeringWorkflow.Application.*` namespaces.
- EngineeringWorkflow API controllers remain in `AssistantEngineer.Api.Controllers.Calculations` with unchanged route/DTO behavior.
- Shared dependency direction is codified in `docs/architecture/module-boundary-matrix.json` and enforced by module-boundary matrix tests.
- Runtime backend projects do not reference `AssistantEngineer.Tools.*` projects.
- Protected endpoint authorization gate remains a stable facade; P8-03A characterization tests freeze status/decision outcomes before internal decomposition.
- Workflow API shell decomposition is staged with controller adapter stability and orchestration migration constraints; P8-03D characterizes workflow shell route/signature/status/response behavior, P8-03E migrates workflow diagnostics/state/submission helpers into module application ownership, and P8-03F reduces main-shell orchestration via API-local action service extraction.
- OwnershipBackfill CLI parser now uses descriptor catalog and argument-reader collaborators (P8-04) while preserving command names, argument names, help output semantics, exit-code meanings, redaction guarantees, and apply-disabled behavior.
- Route inventory closure in P8-05 reduced unknown-classification metadata and tightened ignore-list coverage without changing controller attributes, action signatures, or authorization semantics.
- Scripts/tools rationalization in P8-06 classifies wrapper/tool/workflow surfaces with explicit ReleaseGateCritical and ToolingCritical boundaries.
- Terminology and claims-surface governance in P8-07 is anchored by `docs/architecture/terminology-and-claims-vocabulary.md` and enforced by dedicated claim-surface tests.
- Governance test brittleness reduction in P8-08 adds semantic helper-based assertions for governance docs while preserving strict behavior-level safety checks.
- Final P8 closure decision and deferred P9 backlog are documented in `docs/architecture/p8-engineering-domain-hardening-closure.md`.
- P9-00 validation evidence planning is documented in `docs/validation/engineering-calculation-validation-roadmap.md`, `docs/validation/validation-evidence-inventory.md`, and `docs/validation/validation-claims-policy.md`.
- P9-03 validation fixture provenance governance is documented in `docs/validation/validation-fixture-provenance-model.md` and `docs/validation/validation-fixture-provenance-inventory.md`.
- P9-01 ISO52016 decomposition review/component map are documented in `docs/validation/iso52016-decomposition-review.md` and `docs/validation/iso52016-component-map.md`.
- P9-01A ISO52016 behavior characterization coverage is documented in `docs/validation/iso52016-behavior-characterization-inventory.md`.
- P9-01B ISO52016 matrix/solver seam extraction design and risk register are documented in `docs/validation/iso52016-matrix-solver-seam-design.md` and `docs/validation/iso52016-matrix-solver-seam-risk-register.md`.
- P9-01B1 matrix/solver characterization hardening is documented in `docs/validation/iso52016-matrix-solver-characterization-hardening.md`.

## Known boundary risks

- Workflow and authorization hotspots are decomposition-staged in P8-03; gate characterization and gate-collaborator extraction (P8-03A/B/C), workflow-shell characterization (P8-03D), workflow helper migration (P8-03E), and main controller-shell reduction (P8-03F) are complete.
- ISO52016 service/component decomposition remains staged; P9-01A characterization inventory, P9-01B seam design, and P9-01B1 hardening are complete, while extraction and follow-up review stages P9-01B2/P9-01B3/P9-01B4/P9-01B5/P9-01B6/P9-01C/P9-01D/P9-01E/P9-01F remain pending.
- OwnershipBackfill parser command-specific branches remain in one class and may be optionally split further in future tooling-only stages.
- Infrastructure-to-module dependency surface remains broad and should be reviewed for minimization in later stages.
- Read-history and report/artifact controller partials remain broader than ideal and are optional follow-up decomposition candidates.

## Next review points

- P9-00 engineering calculation validation roadmap refresh.
- P9-03 validation fixture provenance cleanup.
- P9-01 ISO52016 solver/service decomposition review.
- P9-01A ISO52016 behavior characterization inventory.
- P9-01B matrix assembly/solver seam extraction design.
- P9-01B1 matrix assembly/solver characterization hardening.
- P9-04 route inventory deferred-items phase 2.
- P9-05 ownership backfill apply decision remains deferred unless ADR trigger is met.

## Non-claims

- No calculation physics change claim.
- No full donor-model match claim.
- No external simulator match claim.
- No external standard-case validation completion claim.
- No production security certification claim.
- No full tenant isolation claim.
- No ownership backfill execution claim.
- No DB RLS/global EF query filter claim.
