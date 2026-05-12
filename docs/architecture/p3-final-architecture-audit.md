# P3 Final Architecture Audit

## Scope

This document is the final architecture audit for the P3 hardening/refactor cycle.
It records verified implementation status, governance boundaries, and P4 backlog candidates.
It does not introduce new features or new calculation models.

## Verification basis

The audit is based on repository evidence plus regression gates:

```powershell
dotnet build AssistantEngineer.sln -c Debug --no-restore
dotnet test AssistantEngineer.sln -c Debug --no-restore --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-13-building-input-validation-refactor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-14-energy-calculation-pipeline-refactor.ps1
```

Frontend baseline gates (where applicable):

```powershell
npm run test
npm run test:e2e
```

## Done

- P3-01: atomic queued job claim/lease foundation is implemented and guarded.
- P3-02: durable idempotency foundation is implemented on durable workflow persistence with scope+key uniqueness and migration coverage.
- P3-03: workflow snapshot bulk loading boundary is implemented and snapshot-builder per-room N+1 path is removed.
- P3-04: in-memory workflow persistence global SyncRoot locking is removed and concurrency guards are present.
- P3-05: ISO52016 interface naming normalization to `ISo52016*` is implemented and guarded.
- P3-06: frontend E2E smoke baseline (Playwright) is present alongside Vitest/RTL.
- P3-07: `EngineeringWorkflowPersistenceService` hotspot refactor is completed.
- P3-08: `EngineeringCalculationJobService` hotspot refactor is completed.
- P3-09: `EngineeringReportBuilder` hotspot refactor is completed.
- P3-10: `EngineeringWorkflowShell.tsx` hotspot refactor is completed.
- P3-11: `Iso52016PhysicalRoomModelBuilder` hotspot refactor is completed.
- P3-12: `Iso52016MultiZoneHourlySolver` hotspot refactor is completed.
- P3-13: `BuildingInputValidationService` hotspot refactor is completed.
- P3-14: `EnergyCalculationPipelineService` hotspot refactor is completed.
- P3-15: P3 docs/governance status normalization is completed.
- P3-16: full regression gate and release-readiness cleanup is completed.
- P3-17: final architecture audit and P4 backlog definition is completed.

## Partial

- None in the current P3 closure baseline.

## Remaining

- No remaining items inside the committed P3 scope definition.
- Remaining work is tracked as P4 backlog candidates below.

## Hotspots after P3

| Area | File | Current line count | Risk | Recommended P4 action |
| --- | --- | ---: | --- | --- |
| Backend integration tooling | `src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.cs` | 744 | Large integration-oriented mapping surface; higher maintenance and test-fragility risk. | Split export mapping/serialization helpers and add targeted fixture guards. |
| Reporting assembly | `src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportSectionBuilder.cs` | 647 | Section construction density may grow and re-introduce god-builder pressure. | Continue staged decomposition of section families and formatting helpers. |
| ISO52016 hourly core | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016HourlyHeatBalanceCalculator.cs` | 583 | High-risk core numeric flow is concentrated in one file. | Extract non-numeric orchestration helpers while preserving operation order and anchors. |
| Topology aggregation | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Topology/ThermalZoneBoundaryCalculator.cs` | 547 | Complex boundary handling and diagnostics in one class. | Separate boundary classification and diagnostic formatting helpers. |
| Ventilation load engine | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationAndInfiltrationLoadEngine.cs` | 547 | Multiple scenario branches in one engine increase regression risk. | Split scenario branch evaluators and keep shared numeric policy centralized. |
| Natural ventilation loads | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationZoneLoadCalculator.cs` | 540 | Input conditioning and zone-load aggregation are tightly coupled. | Extract input normalization and result aggregation helpers. |
| Frontend workflow API client | `src/Frontend/src/entities/engineering-workflow/api/engineeringWorkflowClient.ts` | 593 | Broad API client surface; risk of drift in error/shape handling. | Split into smaller endpoint modules with shared transport helpers and stable contract tests. |
| Tools entrypoint | `tools/AssistantEngineer.Tools.EngineeringCoreVerification/Program.cs` | 622 | Long CLI orchestration method body reduces maintainability. | Extract command handlers and report writers while keeping command semantics stable. |

## Guard coverage

Current guard/test coverage protects key P3 boundaries:

- Queue claim safety and claim-before-execute boundary.
- Durable idempotency persistence/migration behavior and conflict semantics.
- Snapshot bulk-loading boundary and no N+1 regression for workflow snapshot builder.
- In-memory persistence no-SyncRoot regression.
- ISO52016 interface naming regression guard.
- Hotspot size/facade boundary guards for refactored services/builders.
- Governance/non-overclaim document guards.
- P3 verification wrapper script portability checks.

## Non-claims

- calculation physics unchanged for refactor-only P3 items.
- public API routes unchanged for refactor-only P3 items.
- no full external parity claim.
- no certified compliance claim.
- no claim of ASHRAE 140 validation coverage without separate external evidence.
- no claim that current queue/idempotency implementation is a globally exactly-once distributed system.

## P4 backlog candidates

- Distributed stale-lease recovery for queued jobs.
- Dead-letter queue and advanced retry/backoff scheduler.
- Durable or distributed rate limiting for multi-node deployments.
- Object/blob storage for large trace/report artifacts.
- OpenAPI governance automation expansion.
- Performance benchmark campaign on production-scale datasets.
- External numerical validation beyond internal deterministic anchors.
- Remaining hotspot refactors listed above.
- Frontend broader E2E matrix beyond smoke baseline.
- Observability/log enrichment and deployment runbook hardening.
