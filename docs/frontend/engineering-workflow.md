# Engineering Workflow Foundation

## Purpose

Engineering workflow UI provides a staged path for internal engineering data entry, validation diagnostics, calculation trace summary, and report preview exports.

This is a foundation-level frontend workflow for internal engineering implementation and deterministic validation anchors.

## Supported workflow steps

- Project
- Building metadata
- Zones
- Envelope and boundaries
- Weather and solar readiness
- Ventilation
- Ground
- Domestic hot water
- System energy
- Validation diagnostics
- Calculation trace summary
- Reports
- Review

## Frontend/backend boundary

Frontend does not execute engineering physics.

Frontend aggregates existing backend contracts and endpoints through a typed workflow client.

Calculation logic remains in backend C# calculators and services.

## Client abstraction

Workflow client is implemented in:

- `src/Frontend/src/entities/engineering-workflow/api/engineeringWorkflowClient.ts`

Supported modes:

- `api`: uses workflow backend endpoints:
  - `GET /api/v1/engineering-workflow/{projectId}/state`;
  - `POST /api/v1/engineering-workflow/validate`;
  - `POST /api/v1/engineering-workflow/prepare-calculation`;
  - `POST /api/v1/engineering-workflow/run-calculation`;
  - `GET /api/v1/engineering-workflow/{projectId}/scenarios`;
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}`;
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts`;
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}`;
  - `POST /api/v1/engineering-workflow/trace-preview`;
  - `POST /api/v1/engineering-workflow/report`;
  - `POST /api/v1/engineering-workflow/report/export/json`;
  - `POST /api/v1/engineering-workflow/report/export/markdown`.
- `dev`: explicit internal dev adapter mode for workflow foundation export behavior.

Dev mode is explicit and must not be presented as production-complete workflow execution.

## Diagnostics UX

Workflow diagnostics panel:

- groups diagnostics by severity and step;
- keeps deterministic ordering;
- supports severity filter;
- exposes suggested correction when available;
- allows selecting workflow step from diagnostic.

## Calculation trace UX

Calculation trace panel:

- shows compact module/step summary;
- exposes assumptions and warnings;
- supports summary/standard/detailed toggle;
- avoids large arrays in default workflow view;
- marks detailed endpoint as pending when not wired.

## Report UX

Engineering report preview panel:

- shows report title, sections, warnings, diagnostics, and limitations;
- supports JSON export output;
- supports Markdown export output;
- displays trace appendix summary when requested.

Workflow scenario execution panel:

- runs available modules through backend scenario runner endpoint;
- shows deterministic execution status (`Prepared`, `PartiallyExecuted`, `CompletedWithWarnings`, `FailedValidation`, `FailedExecution`);
- shows module execution status markers without frontend-side physics.

Scenario history panel:

- lists persisted scenario runs by project;
- shows scenario id/status/timestamps and diagnostics count;
- loads persisted scenario artifacts (trace/report/result snapshots);
- keeps empty state explicit when no persisted runs exist.

Persistence status visibility:

- frontend reads backend metadata (`persistence`, `persistenceProvider`, `durablePersistenceEnabled`);
- status is displayed as foundation context only;
- UI does not claim production durability when provider is `InMemory` or foundation SQLite.

## Partial workflow behavior

Workflow supports partial data.

Missing module data is surfaced as diagnostics and incomplete step status instead of causing crash.

## Known limitations

- Frontend workflow is foundation-level.
- Not all production endpoints may be wired yet.
- Frontend does not prove calculation validity.
- Persistence provider may be foundation-level in-memory depending on deployment wiring.
- Frontend runner view may return partial execution when required structured module inputs are missing.
- Report preview summarizes current internal engineering calculations only.
- Report preview is not a legal compliance certificate.
- Report preview is not external validation evidence.
- Report preview does not prove full standard compliance.
- PDF/HTML production rendering is not included in this stage.

## Future production path

To move from foundation to production:

- wire full production calculation runner execution endpoint;
- extend scenario runner persistence/job execution path beyond request-response foundation mode;
- keep frontend client in `api` mode for production paths;
- preserve visible non-claims and diagnostics in normal UI;
- keep validation, trace, and report contracts deterministic and testable.
