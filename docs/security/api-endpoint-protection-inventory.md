# API Endpoint Protection Inventory

## Purpose

This inventory tracks API endpoint protection status during staged authorization rollout and prevents accidental route exposure or accidental mass lock-down without governance updates.

## Scope

The inventory covers controller route groups for:

- projects;
- buildings/floors/rooms/zones;
- calculations and engineering workflow;
- reports and workflow artifacts;
- reference and development endpoints;
- analysis and benchmark endpoints.

## Status model

- `PublicAllowed`
- `DevelopmentOnly`
- `AuthPlanned`
- `AuthPilot`
- `Protected`
- `UnknownNeedsAudit`

## Inventory table

| Controller | Route pattern | Current auth status | Target policy | Rollout stage | Risk | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| ProjectsController | `api/v{version:apiVersion}/projects [GET]` | AuthPilot | ProjectsRead | P5-10 | Read-only project data can be exposed without staged policy checks. | Controlled by read-pilot flags in `ApiAuthorization`. |
| ProjectsController | `api/v{version:apiVersion}/projects/{id:int} [PUT/DELETE]` | AuthPilot | ProjectsWrite | P5-11 | Mutating project routes require staged write authorization. | Controlled by write-pilot flags in `ApiAuthorization`. |
| BuildingsController | `api/v{version:apiVersion}/buildings/{id:int} [GET]` | AuthPilot | BuildingsRead | P5-10 | Building data can disclose scoped engineering context. | Uses building scope resolver in read pilot. |
| BuildingsController | `api/v{version:apiVersion}/buildings/{id:int} [PUT/DELETE]` | AuthPilot | BuildingsWrite | P5-11 | Building mutations are destructive and require staged write policy. | Uses building scope resolver in write pilot. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/prepare-calculation [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Execution endpoints can consume heavy resources and mutate workflow state. | Controlled by execution-pilot flags. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/run-calculation [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Heavy workflow execution should not remain effectively public in protected mode. | Uses workflow/project/building scope fallback. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/jobs [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Job creation/enqueue is execution-sensitive. | Protected by workflow execution gate in pilot mode. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/{projectId:int}/state [GET]` | AuthPilot | WorkflowsRead | P5-14 | Workflow state read can expose scoped diagnostics/history context. | Controlled by workflow-read pilot flags in `ApiAuthorization`. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/scenarios/{scenarioId} [GET]` | AuthPilot | WorkflowsRead | P5-14 | Scenario read can expose scoped workflow history and metadata. | Uses scenario/workflow scope resolution with project/building fallback. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/{projectId:int}/scenarios [GET]` | AuthPilot | WorkflowsRead | P5-14 | Scenario list can enumerate project workflow execution history. | Protected by project-scoped workflow-read gate. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/jobs/{jobId} [GET]` | AuthPilot | WorkflowsRead | P5-14 | Job read can expose scoped status, diagnostics, and artifact references. | Uses job/workflow scope resolution with anti-enumeration behavior. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/jobs/{jobId}/events [GET]` | AuthPilot | WorkflowsRead | P5-14 | Job events can expose scoped workflow history timeline. | Protected by workflow-read gate with job-id + project fallback. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/{projectId:int}/jobs [GET]` | AuthPilot | WorkflowsRead | P5-14 | Job list can expose project-level execution activity. | Protected by project-scoped workflow-read gate. |
| BuildingLoadCalculationsController | `api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Calculation execution endpoints are compute-sensitive. | Protected by calculation gate with building scope. |
| FloorLoadCalculationsController | `api/v{version:apiVersion}/floors/{floorId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Floor calculations can expose scoped execution diagnostics. | Floor->building scope fallback in gate. |
| RoomLoadCalculationsController | `api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Room calculations can expose scoped execution diagnostics. | Room->building scope fallback in gate. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/trace-preview [POST]` | AuthPilot | ReportsRead | P5-13 | Trace preview can expose scoped workflow/report diagnostics. | Controlled by report/artifact pilot and report-read flag. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/report [POST]` | AuthPilot | ReportsWrite | P5-13 | Report generation creates output artifacts and should require staged write policy. | Controlled by report/artifact pilot and report-write flag. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/report/export/* [POST]` | AuthPilot | ReportsWrite | P5-13 | Export routes can disclose scoped report outputs. | Protected by report-write gate. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/scenarios/{scenarioId}/artifacts* [GET]` | AuthPilot | ReportsRead | P5-13 | Artifact reads can expose workflow output content. | Protected by artifact-read gate with workflow/project/building fallback scope. |
| BuildingCoolingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling [GET]` | AuthPilot | ReportsRead | P5-13 | Cooling report reads can expose scoped report context. | Protected by report-read gate using building scope. |
| BuildingCoolingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling/excel [GET]` | AuthPilot | ReportsRead | P5-13 | Cooling report export can expose scoped outputs. | Protected by report-read gate. |
| BuildingHeatingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/heating [GET]` | AuthPilot | ReportsRead | P5-13 | Heating report reads can expose scoped building outputs. | Protected by report-read gate. |
| BuildingEnergyBalanceReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/energy-balance/excel [GET]` | AuthPilot | ReportsRead | P5-13 | Energy-balance export can expose scoped outputs. | Protected by report-read gate. |
| DevelopmentDemoDataController | `api/v{version:apiVersion}/development/demo-data` | AuthPilot | AdministrationManage | P5-09 | Development/demo route must not be exposed in production. | Must remain environment-gated with `IsDevelopment`. |
| BuildingCoolingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling` | AuthPlanned | ReportsRead | P5-04B | Baseline staged report protection inventory entry. | Historical inventory anchor. |
| BuildingEnergyBalanceReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/energy-balance` | AuthPlanned | ReportsRead | P5-04B | Baseline staged report protection inventory entry. | Historical inventory anchor. |
| BuildingHeatingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/heating` | AuthPlanned | ReportsRead | P5-04B | Baseline staged report protection inventory entry. | Historical inventory anchor. |
| StandardTablesController | `api/v{version:apiVersion}/standard-tables` | PublicAllowed | AnonymousAllowed / ProjectsRead | P5-04E | Reference routes can remain public only when explicitly inventoried. | Keep status explicit to avoid accidental policy drift. |
| EquipmentDiagnosticsController | `api/v{version:apiVersion}/equipment-diagnostics/catalog [GET]` | PublicAllowed | ReferenceData | ED-06 | Deterministic diagnostic catalog facets must remain clearly classified and not over-claimed. | Deterministic seed only; no persistence, Telegram, RAG/vector search, AI search, or full manual verification claim. |
| EquipmentDiagnosticsController | `api/v{version:apiVersion}/equipment-diagnostics/error-codes [GET]` | PublicAllowed | ReferenceData | ED-01 | Seeded diagnostic reference data must remain clearly classified and not over-claimed. | Deterministic seed only; no persistence, Telegram, RAG/vector search, AI search, or full manual verification claim. |
| EquipmentDiagnosticsController | `api/v{version:apiVersion}/equipment-diagnostics/cases [GET]` | PublicAllowed | ReferenceData | ED-01 | Seeded diagnostic case data includes safety-sensitive guidance and confidence limitations. | Deterministic seed only; safety notes and non-ManualVerified confidence remain required. |
| EquipmentDiagnosticsController | `api/v{version:apiVersion}/equipment-diagnostics/bot/diagnose [POST]` | PublicAllowed | ReferenceData | ED-15C | Operator-facing diagnostic guidance is safety-sensitive and requires a trained technician; production auth/rate limiting is not claimed by this stage. | Deterministic request limits and runtime-only answers; no persistence, external calls, Telegram, web chat, AI/RAG, manual PDF access, or generated-artifact exposure. |
| EquipmentDiagnosticsTelegramWebhookController | `api/v{version:apiVersion}/equipment-diagnostics/telegram/webhook [POST]` | AuthPlanned | TelegramWebhookSecret | ED-17C | Inbound Telegram webhook can be abused if token, secret, or allowed-chat configuration is compromised. | Disabled by default; requires Telegram secret header; deny wins over allow; chat ID discovery is explicitly controlled and disabled by default; no audit log, allow/deny admin UI, database persistence, or endpoint-specific rate-limit claim. |

Machine-readable canonical inventory is maintained in `docs/security/api-endpoint-protection-inventory.json`.

## Classification model

Inventory classification and required metadata are defined in `docs/security/api-endpoint-classification-model.md`.
Canonical release-boundary posture for claims/non-claims is defined in `docs/security/security-release-boundary.md`.

## P8-05 status note

- P8-05 route inventory deferred-classification closure reduced unknown classifications and tightened ignore-list coverage.
- Deferred entries remain explicit where rollout stage is intentionally not completed.
- No controller route attributes, action signatures, DTO contracts, or authorization semantics were changed in this governance step.
