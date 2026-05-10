# Engineering Workflow Durable Persistence Foundation

## Purpose

Stage 12 adds durable persistence foundation for engineering workflow/project/scenario records while preserving Stage 11 API contracts and in-memory fallback behavior.

Persistence layer remains orchestration storage only and does not execute engineering physics.

## Provider model

Supported providers:

- `InMemory` (foundation/dev/test provider)
- `SQLite` (durable local foundation provider)
- `None` (treated as in-memory fallback in current foundation behavior)

## Configuration

Section: `EngineeringWorkflowPersistence`

Keys:

- `Provider` (`InMemory` or `SQLite`)
- `SqliteConnectionString` (optional for SQLite; if omitted, a local default SQLite file path is used)
- `EnsureCreatedOnStartup` (`true`/`false`) - historical option name kept for compatibility; for SQLite it now applies EF Core migrations on startup.
- `PayloadLimits:*` deterministic payload-size gate settings for persisted JSON/text snapshots.

Environment override pattern can use standard ASP.NET Core configuration mapping, for example:

- `EngineeringWorkflowPersistence__Provider=SQLite`
- `EngineeringWorkflowPersistence__SqliteConnectionString=Data Source=...`
- `EngineeringWorkflowPersistence__PayloadLimits__ArtifactContentMaxBytes=2097152`
- `EngineeringWorkflowPersistence__PayloadLimits__TruncationMarker=[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]`

## Payload size gates

Durable persistence keeps configurable byte limits for request/state/result/diagnostics JSON and artifact content.

When payload limits are exceeded, persistence stores deterministic truncated content and includes marker metadata instead of writing unbounded blobs.

## Stored durable records

- project record
- workflow state snapshots with versions
- scenario request/result summary record
- artifacts (`TraceJson`, `ReportJson`, `ReportMarkdown`, `ValidationDiagnostics`, `ScenarioResultJson`)
- scenario history entries (`Created`, `Prepared`, `Started`, `Completed`, `Failed`, `ReportGenerated`)

## Entity model

Durable provider uses entities with deterministic ordering/indexing:

- `EngineeringProjectEntity`
- `EngineeringWorkflowStateEntity`
- `EngineeringCalculationScenarioEntity`
- `EngineeringCalculationArtifactEntity`
- `EngineeringScenarioHistoryEntryEntity`

Indexes include project/scenario/time/artifact-kind access paths used by workflow API endpoints.

## API behavior compatibility

Stage 11 endpoints remain unchanged:

- `GET /api/v1/engineering-workflow/{projectId}/state`
- `POST /api/v1/engineering-workflow/validate`
- `POST /api/v1/engineering-workflow/prepare-calculation`
- `POST /api/v1/engineering-workflow/run-calculation`
- `GET /api/v1/engineering-workflow/{projectId}/scenarios`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}`

Metadata now includes provider status:

- `persistence`
- `persistenceProvider`
- `durablePersistenceEnabled`

## Migration and schema strategy

Current durable provider uses an initial EF Core migration for the SQLite workflow schema and applies pending migrations when `EnsureCreatedOnStartup=true`. The option name is historical; the initializer no longer calls `EnsureCreated()` for the durable provider.

Production rollout controls, backup policy, and multi-node migration orchestration remain future work.

## Known limitations

- SQLite/local durable provider is foundation-level.
- No production multi-user concurrency guarantee is claimed.
- No background job queue is included.
- No object storage for large artifacts is included yet.
- No full audit/security model is included yet.
- Persistence does not validate engineering correctness.
- Persistence is not a compliance certificate.
- Persistence is not external validation evidence.
- No full standard compliance claim is made.
