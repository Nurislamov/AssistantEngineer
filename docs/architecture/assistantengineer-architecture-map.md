# AssistantEngineer Architecture Map (P8-00)

## Purpose

Provide a stable module/layer map of AssistantEngineer after the P5/P6/P7 governance cycle.

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

## Known boundary risks

- EngineeringWorkflow application namespace/contracts still leak Api naming.
- Module boundary tests do not explicitly cover EngineeringWorkflow assembly yet.
- Large authorization/workflow classes raise long-term boundary erosion risk.

## Next review points

- P8-01 namespace and boundary hardening plan.
- P8-02 architecture-test boundary expansion.
- P8-03 hotspot decomposition design.

## Non-claims

- No calculation physics change claim.
- No full donor-model match claim.
- No external simulator match claim.
- No external standard-case validation completion claim.
- No production security certification claim.
- No full tenant isolation claim.
- No ownership backfill execution claim.
- No DB RLS/global EF query filter claim.
