# P3 Hardening Status

## Scope

P3-01 introduces atomic queued job claim and lease metadata for calculation job worker execution.
P3-02 introduces durable idempotency foundation for heavy engineering workflow submissions.
P3-03 introduces workflow snapshot bulk loading for workflow-state input assembly.
P3-04 introduces in-memory workflow persistence synchronization cleanup without public global SyncRoot locking.
P3-05 introduces ISO52016 interface naming normalization from `IIso52016*` to `ISo52016*`.
P3-06 introduces frontend browser-level E2E smoke baseline using Playwright with mocked workflow API routes.

This is a production-hardening foundation step and does not change engineering calculation physics.

## Implemented in P3-01

- Added explicit repository claim contract for queued jobs:
  - `TryClaimQueuedJobAsync(jobId, workerId, leaseDuration, cancellationToken)`.
- Added claim/lease metadata in persisted job records:
  - `ClaimedByWorkerId`
  - `ClaimedAtUtc`
  - `LeaseExpiresAtUtc`
- Durable SQLite repository now claims via conditional update and validates `rows affected == 1`.
- In-memory repository now applies claim atomically under existing lock for test/dev parity.
- Worker flow now enforces claim-before-execute:
  - list queued candidates,
  - attempt claim,
  - execute only successfully claimed jobs,
  - skip claim failures without noisy errors.
- Worker options now include:
  - `LeaseDurationSeconds`
  - optional `WorkerId`
  - `StaleRunningJobRecoveryEnabled` (currently disabled by default).

## Implemented in P3-02

- Added durable idempotency records table in workflow persistence database:
  - `engineering_workflow_idempotency_records`.
- Added unique scope/key constraint:
  - `(Scope, IdempotencyKey)` unique index.
- Added durable idempotency semantics:
  - first request reserves pending record atomically,
  - same key+scope+same fingerprint replays completed result,
  - same key+scope+different fingerprint returns conflict.
- Durable idempotency is selected automatically when workflow persistence provider is SQLite.
- In-memory idempotency remains available for development/testing provider modes.
- Added migration and tests for:
  - migration/table/index presence,
  - same-key race behavior,
  - replay across service restart with shared SQLite database.

## Implemented in P3-03

- Added bulk snapshot input contract on buildings facade:
  - `GetEngineeringWorkflowBulkInputAsync(buildingId, cancellationToken)`.
- Added bulk workflow input query service in buildings module:
  - loads room engineering inputs once via repository bulk method,
  - maps deterministic room/wall/window snapshots,
  - computes ventilation/ground configured room counts.
- Added repository bulk method:
  - `ListWithEngineeringInputsByBuildingIdAsync(buildingId, cancellationToken)`.
- EF repository implementation now loads room engineering inputs with grouped includes instead of per-room lookup loops.
- `EngineeringWorkflowInputSnapshotBuilder` now consumes one bulk workflow-input query and no longer performs per-room N+1/N+4 calls.
- Added tests and architecture guards for:
  - deterministic ordering,
  - optional data behavior parity,
  - removal of per-room calls in snapshot builder path.

## Implemented in P3-04

- Removed public shared `SyncRoot` from `EngineeringWorkflowMemoryStore`.
- Replaced in-memory workflow persistence backing collections with concurrent dictionaries.
- Removed `lock(_store.SyncRoot)` from in-memory repositories.
- Updated in-memory repositories to use concurrent operations and deterministic read ordering:
  - projects/workflow states/scenarios/artifacts/history/jobs/job-events.
- Preserved atomic in-memory queued-job claim behavior using compare-and-swap (`TryUpdate`) loop in:
  - `InMemoryEngineeringCalculationJobRepository.TryClaimQueuedJobAsync(...)`.
- Added tests and guards for:
  - no SyncRoot usage in in-memory persistence source files,
  - concurrent claim single-winner behavior,
  - concurrent event/history/scenario appends without record loss,
  - deterministic ordering and snapshot read behavior.

## Implemented in P3-05

- Normalized ISO52016 interface declarations from legacy `IIso52016*` naming to `ISo52016*`.
- Updated ISO52016 DI registrations, service constructors, tests, manifests, and engineering docs to use `ISo52016*`.
- Removed legacy `IIso52016*.cs` interface file names from the calculations source tree.
- Added architecture guards to prevent reintroduction of `IIso52016` identifiers and filenames.
- Preserved existing runtime behavior and engineering calculation outputs (naming-only cleanup).

## Implemented in P3-06

- Added Playwright test infrastructure in frontend package (`@playwright/test`, config, and scripts).
- Added browser-level smoke tests:
  - app boot and workflow-shell render,
  - workflow run happy-path with deterministic mocked API responses.
- Kept Vitest/RTL as existing unit/component baseline; Playwright is a separate E2E smoke gate.
- Kept E2E tests backend-independent by mocking workflow endpoints through Playwright route interception.

## Safety boundary

- No new engineering formulas.
- No ISO/EN validation-anchor behavior changes.
- No public API route changes required for this step.
- No calculation physics changes required for this step.

## Remaining out of scope

- distributed stale-lease recovery,
- advanced retry scheduling/backoff,
- dead-letter queue,
- multi-node queue coordinator,
- distributed idempotency cache acceleration,
- background idempotency cleanup worker,
- workflow snapshot cache across services/nodes,
- in-memory persistence durability across process restarts,
- cross-process synchronization for in-memory provider,
- large-scale performance benchmarking on production-size datasets,
- global exactly-once execution guarantees across distributed transactions,
- full audit/security model for job infrastructure.
- full frontend E2E coverage across all pages and failure modes,
- real backend end-to-end integration for frontend smoke tests,
- visual regression matrix and cross-browser matrix expansion.

These remain future hardening steps.
