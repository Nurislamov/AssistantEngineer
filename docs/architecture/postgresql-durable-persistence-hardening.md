# PostgreSQL Durable Persistence Hardening

## Purpose

This document defines the durable persistence hardening baseline for PostgreSQL-backed production deployment.
It captures current architecture inventory and governance policies for migrations, indexes/constraints, transaction boundaries, and payload controls.

## Scope

This hardening baseline covers:

- EF Core DbContexts;
- migrations;
- repository implementations;
- workflow persistence;
- job queue persistence;
- idempotency persistence;
- artifact descriptors/storage metadata;
- transaction boundaries;
- indexes and uniqueness constraints;
- payload size/truncation policy;
- migration smoke testing and operational readiness.

## Non-claims

- No globally exactly-once distributed execution claim.
- No production certification claim.
- No claim that in-memory provider is durable.
- No claim that SQLite provider represents multi-node production PostgreSQL behavior.
- No external compliance/certification claim.

## Provider model

- PostgreSQL: intended durable production provider for core domain persistence (`AppDbContext` in infrastructure).
- SQLite: local durable provider for engineering workflow persistence path in API (`EngineeringWorkflowPersistenceDbContext`).
- InMemory: test/dev-only non-durable provider; not multi-node safe.
- FileSystem artifact storage (P4-07): local provider only; not distributed object storage.

## Current inventory

### DbContexts

| DbContext | Project/Path | Provider support | Migration assembly/path | Main tables/entities | Production readiness note |
| --- | --- | --- | --- | --- | --- |
| `AppDbContext` | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs` | PostgreSQL (`UseNpgsql`) | `src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations` | Projects, Buildings, Floors, Rooms, Windows, Walls, Climate, Schedules, Preferences, Zones, Annual climate/weather | Production-path DB context exists; PostgreSQL integration smoke coverage is partial and needs explicit opt-in rehearsal. |
| `EngineeringWorkflowPersistenceDbContext` | `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | SQLite (`UseSqlite`) | `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/Migrations` | Workflow projects/states/scenarios/artifacts/history/jobs/job events/idempotency records | Durable path is SQLite-focused; PostgreSQL provider for this workflow context is not currently implemented. |

### Durable repositories

| Repository/service | Responsibility | Provider | Transaction behavior | Known risks |
| --- | --- | --- | --- | --- |
| Infrastructure repositories (`ProjectRepository`, `BuildingRepository`, `RoomRepository`, etc.) | Core domain CRUD/read models | PostgreSQL via `AppDbContext` | Per-operation `SaveChangesAsync`; explicit transaction only in development demo seeding flow | Cross-repository multi-step consistency relies on service-layer orchestration; broad transactional boundaries need targeted review. |
| `EfEngineeringProjectRepository` | Workflow project snapshots | SQLite via `EngineeringWorkflowPersistenceDbContext` | Per-operation `SaveChangesAsync` | SQLite semantics are not equivalent to multi-node PostgreSQL behavior. |
| `EfEngineeringWorkflowStateRepository` | Persist workflow state versions | SQLite | Per-operation `SaveChangesAsync` | Unique `(ProjectId, Version)` exists, but no cross-aggregate transaction envelope with artifact saves. |
| `EfEngineeringCalculationScenarioRepository` | Persist scenario request/summary payload | SQLite | Per-operation `SaveChangesAsync` | Large payloads truncated deterministically; not a full blob/object strategy. |
| `EfEngineeringCalculationArtifactRepository` | Persist scenario artifact records | SQLite | Per-operation `SaveChangesAsync` | Stores content as text; large artifact strategy still transitional pending P4-07 integration. |
| `EfEngineeringScenarioHistoryRepository` | Persist scenario history log entries | SQLite | Per-operation `SaveChangesAsync` | Can diverge from artifact/state writes if partial failures happen across steps. |
| `EfEngineeringCalculationJobRepository` | Queue/read/update/claim jobs | SQLite | Atomic claim via conditional `ExecuteUpdateAsync` in `TryClaimQueuedJobAsync` | No explicit dead-letter table yet; stale lease handling is runtime-policy dependent. |
| `EfEngineeringCalculationJobEventRepository` | Persist job lifecycle events | SQLite | Per-operation `SaveChangesAsync` | Event append and job update are separate writes. |
| `EfEngineeringIdempotencyService` | Durable idempotency reserve/replay/complete | SQLite | Atomic uniqueness via DB unique index + insert/update conflict handling (`DbUpdateException`, `ExecuteUpdateAsync`) | Scope/key uniqueness is enforced; cleanup and retention policy needs production tuning. |
| `IEngineeringArtifactStorage` (`InMemoryEngineeringArtifactStorage`, `FileSystemEngineeringArtifactStorage`) | P4-07 artifact storage abstraction for larger artifacts | InMemory/FileSystem | Provider-local atomicity only | Not integrated into existing workflow persistence flow yet; not distributed storage. |

### Migrations

| Migration name/id | DbContext | Purpose | Risk | Notes |
| --- | --- | --- | --- | --- |
| `20260418063333_InitialCreate` | `AppDbContext` | Initial core schema | Medium | Baseline schema for core domain persistence. |
| `20260418181228_AddUniqueIndexes` | `AppDbContext` | Add key uniqueness constraints/indexes | Medium | Correctness-sensitive uniqueness moved to DB. |
| `20260419102140_AddCalculationDomainData` | `AppDbContext` | Add calculation domain entities | Medium | Expanded persistence surface for calculations. |
| `20260419110759_AddClimateDataEntities` | `AppDbContext` | Add climate data persistence | Medium | Data volume and import-path implications. |
| `20260419115418_AddThermalZones` | `AppDbContext` | Add thermal zone structures | Medium | Zone topology dependencies. |
| `20260419171128_ReplaceThermalZoneRoomIdsJsonbWithJoinTable` | `AppDbContext` | Normalize zone-room links | High | Data-shape migration; join-table correctness critical. |
| `20260419184716_AddAnnualClimateData` | `AppDbContext` | Add annual climate model | Medium | Dataset size/read performance sensitivity. |
| `20260420142023_AddWindowShadingParameters` | `AppDbContext` | Add window shading persistence fields | Low | Additive schema change. |
| `20260420142908_AddExpandedHourlyWeatherData` | `AppDbContext` | Expand hourly weather storage | Medium | Volume/performance sensitivity. |
| `20260420143359_AddInfiltrationVentilationParameters` | `AppDbContext` | Add ventilation/infiltration parameters | Medium | Input integrity dependence. |
| `20260420150036_AddIso52016CalculationPreferences` | `AppDbContext` | Add ISO52016 preferences | Medium | Preference defaults impact runtime paths. |
| `20260421172802_RenameRoomOutdoorTemperatureToOverride` | `AppDbContext` | Rename semantic field | Medium | Rename compatibility/mapping checks required. |
| `20260429013955_AddRoomGroundContactMetadata` | `AppDbContext` | Add ground-contact metadata | Medium | Ground-boundary data completeness risk. |
| `20260501160136_SplitHourlyClimateData` | `AppDbContext` | Split hourly climate storage | High | Data transform and read-path implications. |
| `20260510000100_InitialEngineeringWorkflowPersistence` | `EngineeringWorkflowPersistenceDbContext` | Initial workflow durable schema | Medium | SQLite-oriented durable workflow baseline. |
| `20260511000100_AddEngineeringJobClaimLeaseMetadata` | `EngineeringWorkflowPersistenceDbContext` | Add lease claim fields/index | Medium | Queue-claim correctness relies on these fields/indexes. |
| `20260511000200_AddEngineeringWorkflowIdempotencyRecords` | `EngineeringWorkflowPersistenceDbContext` | Add durable idempotency records + unique index | Medium | Conflict/replay correctness depends on unique scope/key. |

### Indexes and constraints

| Table/entity | Index/constraint | Purpose | Required for correctness? | Current status |
| --- | --- | --- | --- | --- |
| `engineering_workflow_states` | Unique `(ProjectId, Version)` | Version monotonicity per project | Yes | Present |
| `engineering_workflow_idempotency_records` | Unique `(Scope, IdempotencyKey)` | Idempotency conflict/replay correctness | Yes | Present |
| `engineering_workflow_jobs` | `LeaseExpiresAtUtc` index | Lease recovery/claim scanning efficiency | Yes (operational) | Present |
| `engineering_workflow_jobs` | `Status` index | Queue polling and status filtering | Yes (operational) | Present |
| `engineering_workflow_artifacts` | `(ScenarioId, ArtifactKind)` index | Artifact lookup by scenario/kind | Yes (operational) | Present (non-unique) |
| `Projects` | Unique `Name` | Prevent duplicate project names | Domain policy dependent | Present |
| `Buildings` | Unique `(ProjectId, Name)` | Prevent duplicate building names in project | Domain policy dependent | Present |
| `Floors` | Unique `(BuildingId, Name)` | Prevent duplicate floor names per building | Domain policy dependent | Present |
| `Rooms` | Unique `(FloorId, Name)` | Prevent duplicate room names per floor | Domain policy dependent | Present |
| `EquipmentCatalogItems` | Unique catalog identity composite | Prevent duplicate catalog entries | Domain policy dependent | Present |

## Transaction boundary policy

- Queued job claim must remain atomic (conditional update pattern or equivalent lock-safe mechanism).
- Idempotency reserve/replay/conflict flow must remain atomic at DB level via uniqueness + conditional updates.
- Workflow state and artifacts should avoid inconsistent partial state; multi-step persistence should use explicit boundaries where needed.
- Do not assume cross-provider transaction equivalence between InMemory/SQLite/PostgreSQL.
- Multi-node correctness must rely on DB-enforced constraints and conditional updates, not in-process locks.
- Retry behavior must be explicit and idempotent.

## Migration policy

- Migrations are append-only.
- Historical migrations must not be edited after merge.
- Every new migration requires smoke test coverage or explicit manual verification notes.
- Destructive migrations require explicit backup/rollback plan in migration PR notes.
- Generated migration files must be reviewed before merge.

## Index/constraint policy

- Idempotency scope/key uniqueness must be DB-enforced.
- Job claim flow should rely on conditional updates and lease metadata indexes.
- Lookup-heavy workflow tables require explicit indexes.
- Index names should remain readable and stable where practical.
- Correctness-critical uniqueness must live in DB constraints, not only in service code.

## Payload/artifact policy

- Workflow payload limits/truncation remain active guardrails for persisted JSON/text.
- Large trace/report/validation payloads should move toward P4-07 artifact storage abstraction.
- DB should not become unbounded blob storage without explicit decision.
- Artifact descriptor/checksum metadata should remain queryable and integrity-verifiable.
- Persistence diagnostics and structured logging should align with `docs/architecture/observability-diagnostics-policy.md`.

## Smoke test strategy

- Level 0: provider-neutral unit tests.
- Level 1: SQLite/local durable smoke tests (current durable workflow baseline).
- Level 2: PostgreSQL integration smoke tests as opt-in until CI/runtime environment supports reliable always-on execution.

## P4-08B Targeted Hardening Result

- Inspected workflow durable persistence query paths in:
  - `EfEngineeringCalculationJobRepository.ListQueuedAsync` and `TryClaimQueuedJobAsync`;
  - `EfEngineeringCalculationArtifactRepository.GetByScenarioAndKindAsync`.
- Applied safe, append-only, non-destructive migration:
  - `20260515000100_AddEngineeringWorkflowQueuedJobAndArtifactLookupIndexes`.
- Added/confirmed indexes:
  - `IX_engineering_workflow_jobs_Status_CancellationRequested_QueuedAtUtc_CreatedAtUtc_Id` for queued/retry polling and deterministic claim order lookups;
  - `IX_engineering_workflow_artifacts_ScenarioId_ArtifactKind_CreatedAtUtc_Id` for deterministic scenario artifact kind lookups by latest timestamp/id.
- Intentionally not changed in P4-08B:
  - No historical migration edits;
  - No table/column renames;
  - No destructive data operations;
  - No uniqueness tightening for artifact `(ScenarioId, ArtifactKind)` because existing runtime semantics rely on deterministic latest-selection and this step avoids data-shape risk.
- Verification command list:
  - `dotnet build AssistantEngineer.sln -c Debug`
  - `dotnet test AssistantEngineer.sln -c Debug`
- Remaining risks after P4-08B:
  - Workflow durable context is still SQLite-only (`RISK-WF-SQLITE-POSTGRES-GAP`);
  - Provider default risk (`RISK-WF-PROVIDER-INMEMORY-DEFAULT`) remains configuration-governed;
  - Cross-aggregate transaction envelope remains future hardening work.

## Future work

- Add opt-in PostgreSQL integration smoke tests for workflow durable persistence.
- Add migration apply/rollback rehearsal workflow for production-like DB snapshots.
- Introduce dead-letter persistence for failed/stale workflow jobs.
- Add stale lease scavenger hardening and observability.
- Add distributed rate limiting/coordination strategy for multi-node workers.
- Add object/blob provider integration for `IEngineeringArtifactStorage`.
- Add DB operation telemetry and alerting for contention/slow query hotspots.
