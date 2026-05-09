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

- `api`: uses available backend endpoints and reports pending endpoint gaps through diagnostics.
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

## Partial workflow behavior

Workflow supports partial data.

Missing module data is surfaced as diagnostics and incomplete step status instead of causing crash.

## Known limitations

- Frontend workflow is foundation-level.
- Not all production endpoints may be wired yet.
- Frontend does not prove calculation validity.
- Report preview summarizes current internal engineering calculations only.
- Report preview is not a legal compliance certificate.
- Report preview is not external validation evidence.
- Report preview does not prove full standard compliance.
- PDF/HTML production rendering is not included in this stage.

## Future production path

To move from foundation to production:

- wire dedicated backend endpoints for trace retrieval and report generation/export;
- keep frontend client in `api` mode for production paths;
- preserve visible non-claims and diagnostics in normal UI;
- keep validation, trace, and report contracts deterministic and testable.
