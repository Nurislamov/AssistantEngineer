# P3 Hardening Status

## Scope

P3 is a hardening/refactor track focused on queue safety, persistence robustness, hotspot decomposition, frontend test baseline, and governance consistency.
P3 changes are architecture-quality steps and do not introduce new engineering physics.

## Status Classification

### Done

- P3-01: atomic queued job claim/lease foundation.
- P3-02: durable idempotency foundation on workflow durable persistence.
- P3-03: workflow snapshot bulk loading boundary with snapshot-builder N+1 path removal.
- P3-04: in-memory workflow persistence cleanup with SyncRoot removal.
- P3-05: ISO52016 interface naming normalization (`IIso52016*` -> `ISo52016*`).
- P3-06: frontend E2E smoke baseline (Playwright) in addition to Vitest/RTL.
- P3-07: `EngineeringWorkflowPersistenceService` hotspot decomposition.
- P3-08: `EngineeringCalculationJobService` hotspot decomposition.
- P3-09: `EngineeringReportBuilder` hotspot decomposition.
- P3-10: `EngineeringWorkflowShell.tsx` hotspot decomposition.
- P3-11: `Iso52016PhysicalRoomModelBuilder` hotspot decomposition.
- P3-12: `Iso52016MultiZoneHourlySolver` hotspot decomposition.
- P3-13: `BuildingInputValidationService` hotspot decomposition.
- P3-14: `EnergyCalculationPipelineService` hotspot decomposition.
- P3-15: status/docs/governance consistency audit.
- P3-16: full regression gate and release-readiness cleanup.
- P3-17: final architecture audit and P4 backlog definition.

### Partially done

- None in the committed P3 closure scope.

### Not done

- None in the committed P3 closure scope.

### Out of scope / deferred

- Distributed stale-lease recovery and lease scavenger for queued jobs.
- Dead-letter queue and advanced retry/backoff scheduler.
- Multi-node queue coordinator.
- Distributed idempotency acceleration and cross-database exactly-once semantics.
- Background idempotency cleanup worker.
- Workflow snapshot cache across services/nodes.
- In-memory provider durability across process restart.
- Object/blob storage for large artifacts.
- Large-scale performance benchmarking on production-size datasets.
- Full frontend E2E matrix, visual regression matrix, and cross-browser matrix expansion.
- Broad persistence architecture rewrite.
- Report schema redesign or visual/PDF subsystem redesign.

## Implemented highlights

### P3-01 Queue claim safety

- Explicit repository claim contract `TryClaimQueuedJobAsync(...)`.
- Claim metadata fields: `ClaimedByWorkerId`, `ClaimedAtUtc`, `LeaseExpiresAtUtc`.
- Durable claim uses conditional update with rows-affected semantics.
- Worker enforces claim-before-execute and skips failed claims.

### P3-02 Durable idempotency foundation

- Durable idempotency records table on workflow persistence path.
- Scope/key uniqueness (`Scope, IdempotencyKey`) and request fingerprint conflict handling.
- Durable replay behavior for matching key/scope/fingerprint.

### P3-03 Snapshot bulk loading

- Bulk workflow snapshot input boundary.
- Snapshot-builder per-room N+1/N+4 fetch path removed.
- Deterministic ordering and optional data behavior guarded by tests.

### P3-04 In-memory persistence cleanup

- Public shared `SyncRoot` removed from workflow in-memory persistence.
- `lock(_store.SyncRoot)` usage removed from workflow in-memory repositories.
- Atomic in-memory queued claim behavior preserved.

### P3-05 ISO52016 naming cleanup

- ISO52016 interface names normalized to `ISo52016*`.
- Legacy `IIso52016*.cs` interface files removed.

### P3-06 Frontend smoke baseline

- Playwright smoke baseline added with mocked workflow API routes.
- Vitest/RTL retained as unit/component baseline.

### P3-07 through P3-14 hotspot refactors

- Targeted decomposition completed for workflow persistence, job service, report builder, frontend shell, physical room model builder, multi-zone solver, building input validation service, and energy calculation pipeline service.
- Refactor scope kept at facade/orchestration boundaries with no intentional physics changes.

### P3-15 through P3-17 governance closure

- P3 status/governance docs normalized and guarded.
- Release-readiness checklist and P3 verify wrappers kept portable.
- Final P3 architecture audit and P4 backlog recorded.

## Safety boundary

- No new engineering formulas.
- No solver equation redesign.
- No timestep semantics changes.
- No public API route changes required by P3 hardening steps.
- No public DTO/API response format changes required by P3 hardening steps.
