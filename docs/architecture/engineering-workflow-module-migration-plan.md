# Engineering Workflow Module Migration Plan

## Scope and intent
- Date: 2026-05-13.
- Goal of this step: inventory + boundary plan + safe module skeleton.
- Non-goals for this step:
- No public API route changes.
- No response JSON shape changes.
- No calculation physics changes.
- No mass production code move.
- No persistence implementation extraction yet.
- No exactly-once/distributed processing claims.

## Progress update (P1 extraction)
- Date: 2026-05-13.
- Status: first safe non-HTTP contracts/builders extraction completed.
- Moved from API to module (`src/Backend/AssistantEngineer.Modules.EngineeringWorkflow`):
- Contracts:
- `EngineeringWorkflowDtos.cs`
- `EngineeringCalculationScenarioDtos.cs`
- `EngineeringCalculationJobDtos.cs`
- Builders/services:
- `EngineeringWorkflowCatalog.cs`
- `EngineeringWorkflowInputSnapshot.cs`
- `IEngineeringWorkflowInputSnapshotBuilder.cs`
- `EngineeringWorkflowInputSnapshotBuilder.cs`
- `IEngineeringWorkflowTracePreviewService.cs`
- `EngineeringWorkflowTracePreviewService.cs`
- `IEngineeringWorkflowReportPreviewService.cs`
- `EngineeringWorkflowReportPreviewService.cs`
- Compatibility mode:
- Contract and moved service namespaces are kept unchanged in this phase to avoid risky mass refactors and preserve API behavior.

## Progress update (P2 job/idempotency policy extraction)
- Date: 2026-05-13.
- Status: pure job lifecycle/idempotency policies and abstractions extracted.
- Moved from API to module (`src/Backend/AssistantEngineer.Modules.EngineeringWorkflow`):
- Job policies/services:
- `Application/Jobs/EngineeringCalculationJobStatusTransitionPolicy.cs`
- `Application/Jobs/EngineeringCalculationJobPayloadCodec.cs`
- `Application/Jobs/EngineeringCalculationJobEventRecorder.cs`
- Persistence abstraction used by job event recorder:
- `Application/Persistence/IEngineeringCalculationJobEventRepository.cs`
- Idempotency abstractions/pure services:
- `Application/Idempotency/IEngineeringIdempotencyService.cs`
- `Application/Idempotency/EngineeringIdempotencyModels.cs`
- `Application/Idempotency/EngineeringIdempotencyRequestFingerprint.cs`
- `Application/Idempotency/EngineeringIdempotencyOptions.cs`
- `Application/Idempotency/InMemoryEngineeringIdempotencyService.cs`
- Left in API intentionally in this step:
- `EfEngineeringIdempotencyService.cs` (durable persistence adapter).
- `EngineeringIdempotencyServiceRegistration.cs` (API composition root selection of durable vs in-memory provider).

## Inventory (`src/Backend/AssistantEngineer.Api/Services/Calculations/**`)

### 1) Workflow state builders (12 files)
- `Workflow/EngineeringWorkflowStateBuilder.cs`
- `Workflow/IEngineeringWorkflowStateBuilder.cs`
- `Workflow/EngineeringWorkflowInputSnapshotBuilder.cs`
- `Workflow/IEngineeringWorkflowInputSnapshotBuilder.cs`
- `Workflow/EngineeringWorkflowDiagnosticsService.cs`
- `Workflow/IEngineeringWorkflowDiagnosticsService.cs`
- `Workflow/EngineeringWorkflowTracePreviewService.cs`
- `Workflow/IEngineeringWorkflowTracePreviewService.cs`
- `Workflow/EngineeringWorkflowReportPreviewService.cs`
- `Workflow/IEngineeringWorkflowReportPreviewService.cs`
- `Workflow/EngineeringWorkflowCatalog.cs`
- `Workflow/EngineeringWorkflowInputSnapshot.cs`

### 2) Scenario execution (20 files)
- `EngineeringCalculationScenarioRunner.cs`
- `IEngineeringCalculationScenarioRunner.cs`
- `ScenarioExecution/*` (scenario step execution, module executor, request validator, result builder, outcomes and contracts).

### 3) Job lifecycle and orchestration (8 files)
- `EngineeringCalculationJobService.cs`
- `IEngineeringCalculationJobService.cs`
- `Jobs/EngineeringCalculationJobWorker.cs`
- `Jobs/EngineeringCalculationJobWorkerOptions.cs`
- `Jobs/EngineeringCalculationJobExecutionOrchestrator.cs`
- `Jobs/EngineeringCalculationJobStatusTransitionPolicy.cs` (moved to module in P2)
- `Jobs/EngineeringCalculationJobEventRecorder.cs` (moved to module in P2)
- `Jobs/EngineeringCalculationJobPayloadCodec.cs` (moved to module in P2)

### 4) Idempotency (7 files)
- `Idempotency/IEngineeringIdempotencyService.cs` (moved to module in P2)
- `Idempotency/InMemoryEngineeringIdempotencyService.cs` (moved to module in P2)
- `Idempotency/EfEngineeringIdempotencyService.cs`
- `Idempotency/EngineeringIdempotencyModels.cs` (moved to module in P2)
- `Idempotency/EngineeringIdempotencyOptions.cs` (moved to module in P2)
- `Idempotency/EngineeringIdempotencyRequestFingerprint.cs` (moved to module in P2)
- `Idempotency/EngineeringIdempotencyServiceRegistration.cs`

### 5) Persistence abstractions and implementations (37 files)
- `Persistence/I*Repository.cs` and `IEngineeringWorkflowPersistenceService.cs` abstractions.
- `Persistence/InMemory*Repository.cs` implementations + memory store.
- `Persistence/Durable/*` EF DbContext, entities, repositories, database initializer, migrations.
- `Persistence/EngineeringWorkflowPersistenceService.cs` and payload/artifact helpers.
- `Persistence/EngineeringWorkflowPersistenceRegistration.cs` composition and provider switch.

### 6) HTTP-specific helpers (0 files in this subtree by code scan)
- No direct `AspNetCore` HTTP primitives were found in `Services/Calculations/**`.
- HTTP transport/composition stays in API controllers/filters/configuration (`Controllers/*`, `Configuration/*`), not in these files.

## Boundary target

## What remains in API
- Controllers, routing attributes, endpoint filters, `ActionResult` shaping.
- API composition root and module wiring (`Configuration/*`).
- HTTP contract adapters only (request binding, response mapping, policy attributes).

## What moves to `AssistantEngineer.Modules.EngineeringWorkflow` (Application)
- Workflow state building and diagnostics flow.
- Scenario orchestration (runner, executor, validator, result builder, step coordination).
- Job lifecycle orchestration and status transitions.
- Idempotency service interfaces + orchestration logic (provider-agnostic behavior).
- Contracts/abstractions consumed by API controller layer.

## What moves to Infrastructure (future phase, not this step)
- EF DbContext/entities/migrations for engineering workflow persistence.
- Durable/in-memory repository implementations.
- Provider switching and persistence registrations.
- DB initialization and payload storage/compaction implementation details.

## Proposed migration sequence
1. Create module shell (`AssistantEngineer.Modules.EngineeringWorkflow`) with no-op DI entrypoint.
2. Introduce boundary guard (soft mode) to prevent untracked new workflow application services in API.
3. Move interfaces/contracts first (or add forwarding wrappers) while keeping API routes and DTO responses identical.
4. Move pure orchestration services (state builders, scenario runner/executor, job orchestration).
5. Keep persistence implementations in API/Infrastructure adapter layer until dedicated extraction phase.
6. Shift API composition to resolve services from module namespaces.
7. Remove temporary adapters only after tests and contract snapshots are stable.

## Risk matrix
- Risk: Route/response contract drift.
- Impact: High.
- Mitigation: Keep controller signatures and DTO mapping unchanged; run API tests and documentation guards.
- Risk: Behavior drift in scenario/job orchestration.
- Impact: High.
- Mitigation: Move in thin slices; preserve existing service logic; add focused regression tests before each extraction.
- Risk: Persistence coupling blocks extraction.
- Impact: Medium.
- Mitigation: Extract orchestration first behind repository abstractions; defer implementation move.
- Risk: Background worker lifecycle regressions.
- Impact: Medium.
- Mitigation: Keep worker hosting in API for now; only move orchestration internals first.
- Risk: Namespace churn and merge conflicts.
- Impact: Medium.
- Mitigation: Avoid mass rewrite; incremental file-by-file migration with temporary adapters.

## Test strategy
- Mandatory on each slice:
- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- Keep existing architecture/API documentation guard tests green.
- Add or update focused tests for each extracted service boundary before deleting old API implementations.
- Keep contract/API shape assertions as the safety net for route/JSON stability.

## No public route change policy
- Controller routes, action names, and API versions remain unchanged.
- Response DTO JSON fields and casing remain unchanged.
- Any migration PR that changes route templates or response shapes is out of scope for this phase.

## Soft architecture guard policy
- Guard objective: new workflow/scenario/job application-service-like files should not be added in `Api/Services/Calculations` silently.
- Enforcement mode: allowlist + explicit exception registry.
- Exception policy: temporary only, with reason/owner/date in `docs/architecture/engineering-workflow-api-service-exceptions.md`.

## Next candidates after P1
- `IEngineeringWorkflowDiagnosticsService` + `EngineeringWorkflowDiagnosticsService`:
- Blocker: direct dependency on `EngineeringWorkflowPersistenceProviderInfo` from API persistence layer.
- `IEngineeringWorkflowStateBuilder` + `EngineeringWorkflowStateBuilder`:
- Blocker: currently coupled to diagnostics service still in API.
- `IEngineeringWorkflowSubmissionService` + `EngineeringWorkflowSubmissionService`:
- Blocker: direct dependency on idempotency and persistence adapters still hosted in API layer.
- Job/scenario orchestration core (`EngineeringCalculationScenarioRunner`, `EngineeringCalculationJobService`, `Jobs/EngineeringCalculationJobExecutionOrchestrator.cs`, `Jobs/EngineeringCalculationJobWorker.cs`, `ScenarioExecution/*`):
- Blocker: requires incremental persistence-boundary extraction to avoid risky big-bang move.
- Durable idempotency adapter extraction (`EfEngineeringIdempotencyService` + provider selection wiring):
- Blocker: coupled to EF persistence context and provider-specific runtime behavior; move after persistence boundary split.
