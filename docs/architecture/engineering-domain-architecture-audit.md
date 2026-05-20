# Engineering/Domain Architecture Audit (P8-00)

## Purpose

This audit records the post-P6/P7 engineering and domain architecture state, highlights maintainability and boundary risks, and prepares a staged hardening backlog without changing runtime behavior.

## Scope

This audit covers:

- backend module and layer inventory;
- domain/application/infrastructure/api boundaries;
- SOLID, KISS, DRY, DDD and naming quality;
- legacy/dead code and test candidates;
- scripts versus C# tools boundaries;
- engineering calculation module architecture and claim hygiene;
- governance/documentation claim consistency.

## Non-claims

- No calculation physics change claim.
- No full donor-model match claim.
- No external simulator match claim.
- No external standard-case validation completion claim.
- No production security certification claim.
- No full tenant isolation claim.
- No ownership backfill execution claim.
- No DB RLS/global EF query filter claim.

## Current architecture snapshot

The repository has a modular backend (`AssistantEngineer.Modules.*`), API host (`AssistantEngineer.Api`), shared infrastructure (`AssistantEngineer.Infrastructure`), and a large governance/tooling surface added through P5/P6/P7. Runtime ownership-backfill apply/write path remains intentionally disabled.

## Module/layer inventory

- API layer: `src/Backend/AssistantEngineer.Api`
- Infrastructure layer: `src/Backend/AssistantEngineer.Infrastructure`
- Domain/Application modules: `src/Backend/AssistantEngineer.Modules.*`
- Shared kernel: `src/Backend/AssistantEngineer.SharedKernel`
- Tools: `tools/AssistantEngineer.Tools.*`
- Scripts: `scripts/engineering-core`, `scripts/iso52016`
- Tests: `tests/AssistantEngineer.Tests`
- Governance/docs: `docs/security`, `docs/adr`, `docs/architecture`

## SOLID findings

- `ProtectedEndpointAuthorizationGate` is a hotspot with multiple concerns (option gates, permission evaluation, message shaping).
- `OwnershipBackfillCommandLineParser` has very broad parsing/validation responsibility in one class.
- `EngineeringWorkflowController` partial surface is large and blends orchestration/read-history/report endpoints in one controller shell.

## KISS findings

- Governance chain is strong but operationally complex; many stage artifacts require cross-reference to interpret current boundary.
- Repeated status-note blocks in long readiness docs increase cognitive load for operators.

## DRY findings

- Repeated documentation assertions and phrase checks still exist across architecture/governance tests (partially consolidated in P7-02, but not fully).
- Route/claims status text remains duplicated between some inventories and stage documents.

## DDD/naming findings

- `AssistantEngineer.Modules.EngineeringWorkflow/Application` currently uses `AssistantEngineer.Api.*` namespaces/contracts for several application artifacts, which weakens clean module naming and bounded-context clarity.
- Several broad `*Service` names combine orchestration and policy responsibilities (for example workflow/report builder layers).

## Legacy/dead code findings

- Multiple routes remain explicitly deferred/unknown in protection inventory metadata and ignore list; these are governance debt rather than immediate runtime risk.
- Placeholder `TODO` metadata in EnergyPlus fixture authoring tool indicates unfinished authoring provenance scaffolding.

## Scripts/tools findings

- Most engineering-core and iso52016 scripts are wrappers around C# tools; this is useful for operator ergonomics but increases wrapper surface maintenance.
- Some script wrappers overlap in verification/regeneration intent and need consolidation review.

## Tests architecture findings

- Architecture/governance test suite is large; many tests provide real guardrails, but wording-sensitive assertions remain brittle in places.
- Module boundary tests currently omit explicit `AssistantEngineer.Modules.EngineeringWorkflow` coverage, reducing boundary confidence for that module.

## Engineering calculation findings

- Calculation implementation is in C# modules/services (not in scripts), which keeps runtime physics in compiled code.
- ISO52016/solar/weather/report components are substantial and sometimes concentrated in large classes, suggesting future decomposition opportunities.
- Documentation and verification artifacts consistently use non-claim language for EnergyPlus/ASHRAE scope boundaries.

## Documentation/claims findings

- No positive false claim was found for full tenant isolation, production apply enabled, or production security certification in audited security governance docs.
- There is still broad documentation volume and overlap, which increases drift risk even with index/vocabulary normalization.

## Risk summary

- Critical runtime safety regressions were not observed in this audit step.
- Highest current risks are maintainability and boundary clarity (especially EngineeringWorkflow layering and missing module-boundary coverage).
- Governance and documentation scale are now a primary operational risk factor rather than missing baseline controls.

## Recommended P8 backlog

- P8-01: EngineeringWorkflow namespace and boundary hardening plan (audit-to-refactor bridge).
- P8-02: Module-boundary test coverage expansion for EngineeringWorkflow and related contracts.
- P8-03: Authorization/workflow hotspot decomposition plan (`ProtectedEndpointAuthorizationGate`, workflow controller shell).
- P8-04: OwnershipBackfill CLI parser/validator decomposition with behavior-lock tests.
- P8-05: Route inventory deferred classification reduction plan.
- P8-06: Scripts-to-tools rationalization (wrapper minimization and ownership map).
- P8-07: Engineering terminology and validation-claim contract cleanup.
- P8-08: Governance test brittleness reduction (wording-coupling minimization).

## Next steps

Start with P8-01 to resolve the highest boundary risk (EngineeringWorkflow layering/naming), then P8-02 to close architecture-test blind spots before larger refactor stages.
