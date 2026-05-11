# P3 Hardening Status

## Scope

P3-01 introduces atomic queued job claim and lease metadata for calculation job worker execution.
P3-02 introduces durable idempotency foundation for heavy engineering workflow submissions.
P3-03 introduces workflow snapshot bulk loading for workflow-state input assembly.
P3-04 introduces in-memory workflow persistence synchronization cleanup without public global SyncRoot locking.
P3-05 introduces ISO52016 interface naming normalization from `IIso52016*` to `ISo52016*`.
P3-06 introduces frontend browser-level E2E smoke baseline using Playwright with mocked workflow API routes.
P3-07 introduces hotspot refactor phase 1 for workflow persistence payload/artifact responsibilities.
P3-08 introduces hotspot refactor phase 2 for engineering calculation job lifecycle orchestration.
P3-09 introduces hotspot refactor phase 3 for engineering report builder decomposition.
P3-10 introduces frontend workflow shell hotspot decomposition with extracted hook and focused view components.
P3-11 introduces ISO 52016 physical room model builder hotspot decomposition into focused physical input/mapping components.

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

## Implemented in P3-07

- Decomposed `EngineeringWorkflowPersistenceService` into focused helpers without changing public API behavior:
  - `EngineeringWorkflowPersistencePayloadService`
  - `EngineeringWorkflowArtifactPersistenceService`
- Kept persistence orchestration in `EngineeringWorkflowPersistenceService` as a facade.
- Preserved payload limit and deterministic truncation behavior for:
  - workflow state payload,
  - scenario request/result/diagnostics payloads,
  - artifact content payloads.
- Added focused tests for extracted payload/artifact components and architecture guard on hotspot size.

## Implemented in P3-08

- Decomposed `EngineeringCalculationJobService` into focused lifecycle components while preserving facade entrypoints:
  - `EngineeringCalculationJobStatusTransitionPolicy`
  - `EngineeringCalculationJobEventRecorder`
  - `EngineeringCalculationJobExecutionOrchestrator`
  - `EngineeringCalculationJobPayloadCodec`
- Centralized job status transition mapping and terminal/cancellation transition behavior.
- Centralized job event persistence and deterministic event diagnostics serialization.
- Separated claimed/synchronous execution orchestration from the facade while keeping:
  - P3-01 atomic claim-before-execute semantics,
  - P3-02 idempotency request semantics and route-level behavior.
- Preserved existing public API routes and calculation runner usage (no controller execution logic and no calculation physics changes).
- Added tests and guard coverage for:
  - lifecycle policy transitions,
  - event recorder behavior,
  - claimed execution regression path,
  - hotspot size/facade boundary.

## Implemented in P3-09

- Decomposed `EngineeringReportBuilder` into focused components while preserving report endpoints and report-generation behavior:
  - `EngineeringReportSectionBuilder`
  - `EngineeringReportDiagnosticsSectionBuilder`
  - `EngineeringReportFormattingService`
  - `EngineeringReportSectionSelectionPolicy`
- Kept `EngineeringReportBuilder` as facade/entrypoint with deterministic section orchestration.
- Preserved section ordering, report metadata keys, diagnostics aggregation behavior, and summary value mapping semantics.
- Added focused tests for:
  - section builders with missing-data and diagnostics behavior,
  - formatting helper deterministic behavior,
  - hotspot size/facade architecture guard.

## Implemented in P3-10

- Decomposed `EngineeringWorkflowShell.tsx` into focused frontend components without changing backend routes:
  - `useEngineeringWorkflowShell` (workflow UI state/actions orchestration),
  - `EngineeringWorkflowStepContent` (step-specific content rendering),
  - `engineeringWorkflowShellViewModel` (pure view-model helpers).
- Kept `EngineeringWorkflowShell` as a feature facade/container.
- Preserved workflow UX behavior for:
  - loading/error/query state handling,
  - run/prepare/report actions,
  - diagnostics, trace, report preview, scenario history, and job panel rendering.
- Added frontend tests for:
  - shell loading/error/render-action regression,
  - extracted hook behavior,
  - extracted view-model helper behavior.
- Kept Vitest/RTL and Playwright smoke coverage green after decomposition.

## Implemented in P3-11

- Decomposed `Iso52016PhysicalRoomModelBuilder` into focused physical-model components while preserving its facade entrypoint:
  - `Iso52016PhysicalRoomModelValidation`
  - `Iso52016PhysicalRoomModelMapping`
  - `Iso52016PhysicalThreeNodeRequestBuilder`
  - `Iso52016PhysicalSurfaceExpandedRequestBuilder`
  - `Iso52016PhysicalRoomModelRequestFactory`
- Preserved deterministic physical model assembly behavior for:
  - aggregated three-node fallback path,
  - explicit surface-expanded path,
  - operation-profile ventilation and gain split mapping,
  - boundary condition mapping and fallback temperature selection,
  - validation/default/fraction guard behavior.
- Added architecture guard coverage to keep `Iso52016PhysicalRoomModelBuilder` as a thin orchestration facade and prevent regression to god-builder size.
- No solver behavior changes and no calculation physics updates; this is internal decomposition only.

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
- no DB schema rewrite for workflow persistence,
- no object/blob storage migration for artifacts,
- no distributed artifact storage provider,
- no broad persistence architecture rewrite.
- no distributed stale-lease recovery or dead-letter queue,
- no advanced retry scheduler/backoff strategy,
- no cross-node exactly-once execution guarantee.
- no report schema redesign or new report format family,
- no visual/PDF report rendering subsystem redesign,
- no object-storage migration for report artifacts.
- no broad frontend redesign or routing rewrite,
- no full browser E2E matrix expansion,
- no backend API contract change for workflow shell decomposition.

These remain future hardening steps.
