# Engineering Workflow Persistence Foundation

## Purpose

Engineering workflow persistence foundation provides a deterministic storage layer for workflow state snapshots, scenario request/result records, trace/report artifacts, and scenario history events.

This layer is orchestration and storage only. It does not implement engineering physics.

## Persisted records

- `EngineeringProjectRecord`
- `EngineeringWorkflowStateRecord`
- `EngineeringCalculationScenarioRecord`
- `EngineeringCalculationArtifactRecord`
- `EngineeringScenarioHistoryEntry`
- `EngineeringCalculationJobRecord`
- `EngineeringCalculationJobEventRecord`

## Scenario lifecycle

1. `prepare-calculation` creates a prepared scenario record and diagnostics artifacts.
2. `run-calculation` creates or updates scenario execution record with compact summary.
3. Trace/report/result artifacts are saved by artifact kind.
4. History events are appended (`Created`, `Prepared`, `Started`, `Completed`, `Failed`, `ReportGenerated`).
5. Job lifecycle records/events are appended (`Created`, `Queued`, `Running`, terminal/cancel states).

## Artifact kinds

- `TraceJson`
- `ReportJson`
- `ReportMarkdown`
- `ValidationDiagnostics`
- `ScenarioResultJson`

## API endpoints

- `GET /api/v1/engineering-workflow/{projectId}/state`
- `GET /api/v1/engineering-workflow/{projectId}/scenarios`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}`
- `POST /api/v1/engineering-workflow/prepare-calculation`
- `POST /api/v1/engineering-workflow/run-calculation`
- `POST /api/v1/engineering-workflow/jobs`
- `GET /api/v1/engineering-workflow/jobs/{jobId}`
- `GET /api/v1/engineering-workflow/jobs/{jobId}/events`
- `POST /api/v1/engineering-workflow/jobs/{jobId}/cancel`
- `GET /api/v1/engineering-workflow/{projectId}/jobs`

## Provider model

Current foundation supports provider selection:

- `InMemory` for deterministic dev/test fallback behavior;
- `SQLite` for local durable persistence foundation;
- `None` which falls back to deterministic in-memory behavior in current stage.

Configuration section: `EngineeringWorkflowPersistence`.

Primary keys:

- `Provider`
- `SqliteConnectionString`
- `EnsureCreatedOnStartup`

## Determinism rules

- workflow state and scenario snapshots are serialized in stable JSON shape;
- diagnostics are sorted and deduplicated before persistence;
- compact scenario summaries are stored in scenario record;
- full trace/report content is stored in artifacts;
- no local file writes are required for persistence operations.

## Known limitations

- Durable provider may still be SQLite/local foundation-level in current deployment wiring.
- Persistence may be in-memory when provider selection is not configured for durable storage.
- Persistence does not validate calculation correctness.
- Artifacts summarize internal engineering calculations only.
- Workflow persistence is not a compliance certificate.
- Workflow persistence is not external validation evidence.
- Workflow persistence provides no full standard compliance claim.
- Background job queue execution is not part of this stage.
