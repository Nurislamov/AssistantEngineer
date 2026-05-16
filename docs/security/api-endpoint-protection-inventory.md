# API Endpoint Protection Inventory

## Purpose

This inventory tracks API endpoint protection status during staged authorization rollout and prevents accidental route exposure or accidental mass lock-down without governance updates.

## Scope

The inventory covers controller route groups for:

- projects;
- buildings/floors/rooms/zones;
- calculations and engineering workflow;
- reports;
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
| ProjectsController | `api/v{version:apiVersion}/projects` | AuthPlanned | ProjectsRead / ProjectsWrite | P5-04B/P5-04C | Project data can be accessed by identifier without full principal scoping rollout. | Inventory anchor for project read/write protection. |
| ProjectsController | `api/v{version:apiVersion}/projects [GET]` | AuthPilot | ProjectsRead | P5-10 | Read-only project list should not remain effectively public when pilot is enabled. | Controlled by `ApiAuthorization:EnableReadEndpointProtectionPilot` + `RequireProjectReadAuthorization`. |
| ProjectsController | `api/v{version:apiVersion}/projects/{id:int} [GET]` | AuthPilot | ProjectsRead | P5-10 | Project-by-id reads can disclose scoped engineering context. | Tenant mismatch behavior is controlled by `ApiAuthorization:ReturnNotFoundForTenantMismatch`. |
| ProjectsController | `api/v{version:apiVersion}/projects [POST]` | AuthPilot | ProjectsWrite | P5-11 | Project creation mutates scoped state and requires staged write authorization. | Controlled by `ApiAuthorization:EnableWriteEndpointProtectionPilot` + `RequireProjectWriteAuthorization`. |
| ProjectsController | `api/v{version:apiVersion}/projects/{id:int} [PUT]` | AuthPilot | ProjectsWrite | P5-11 | Project updates should not remain effectively public in production authorization mode. | Uses project-scope write gate with tenant mismatch behavior via `ReturnNotFoundForTenantMismatch`. |
| ProjectsController | `api/v{version:apiVersion}/projects/{id:int} [DELETE]` | AuthPilot | ProjectsWrite | P5-11 | Project delete operations are destructive and require explicit write permission. | Protected by project-scope write gate when pilot options are enabled. |
| BuildingsController | `api/v{version:apiVersion}/buildings` | AuthPlanned | BuildingsRead / BuildingsWrite | P5-04B/P5-04C | Building write paths need staged authorization to avoid breaking existing workflow. | Coordinate with tenant-scoping policy and project ownership checks. |
| BuildingsController | `api/v{version:apiVersion}/buildings/{id:int} [GET]` | AuthPilot | BuildingsRead | P5-10 | Building-by-id reads can expose scoped building context when pilot protections are off. | Controlled by `ApiAuthorization:EnableReadEndpointProtectionPilot` + `RequireBuildingReadAuthorization`. |
| BuildingsController | `api/v{version:apiVersion}/projects/{projectId:int}/buildings [GET]` | AuthPilot | BuildingsRead | P5-10 | Project-scoped building listing should require staged read authorization. | Uses project scope resolution in the read pilot while preserving default compatibility. |
| BuildingsController | `api/v{version:apiVersion}/projects/{projectId:int}/buildings [POST]` | AuthPilot | BuildingsWrite | P5-11 | Building creation mutates project-scoped state and requires staged write protection. | Uses parent project scope with `BuildingsWrite` during write pilot. |
| BuildingsController | `api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype [POST]` | AuthPilot | BuildingsWrite | P5-11 | Archetype-based building creation must not bypass write authorization. | Protected by parent project scope gate in write rollout. |
| BuildingsController | `api/v{version:apiVersion}/buildings/{id:int} [PUT]` | AuthPilot | BuildingsWrite | P5-11 | Building updates alter engineering model state and require write permission. | Uses building-scope write gate when pilot is enabled. |
| BuildingsController | `api/v{version:apiVersion}/buildings/{id:int} [DELETE]` | AuthPilot | BuildingsWrite | P5-11 | Building delete operations are destructive and require explicit authorization. | Protected by building-scope write gate with compatibility defaults. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow` | AuthPlanned | WorkflowsRead / WorkflowsExecute | P5-04C | Workflow execution endpoints are high-impact and must be protected in controlled waves. | Frontend compatibility path currently remains anonymous-compatible by default. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/prepare-calculation [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Workflow preparation path creates execution context and should require staged execute authorization. | Controlled by `ApiAuthorization:EnableExecutionEndpointProtectionPilot` + `RequireWorkflowExecuteAuthorization`. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/run-calculation [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Heavy workflow run endpoint should not remain anonymously executable in protected environments. | Uses reusable execution authorization gate with workflow/project/building fallback scope checks. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/jobs [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Job enqueue/start route can trigger heavy execution workload and needs execute permission. | Protected when execution pilot flags are enabled; default compatibility remains unchanged. |
| EngineeringWorkflowController | `api/v{version:apiVersion}/engineering-workflow/jobs/{jobId}/cancel [POST]` | AuthPilot | WorkflowsExecute | P5-12 | Job cancel route mutates workflow execution state. | Protected by execution gate; workflow-id ownership mapping remains staged for future rollout. |
| BuildingLoadCalculationsController | `api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Building load-calculation routes are compute-heavy and should require principal execute permission in protected mode. | Controlled by `ApiAuthorization:EnableExecutionEndpointProtectionPilot` + `RequireCalculationRunAuthorization`. |
| FloorLoadCalculationsController | `api/v{version:apiVersion}/floors/{floorId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Floor-level load calculations should not remain effectively public in protected environments. | Uses floor-to-building scope fallback via reusable authorization gate. |
| RoomLoadCalculationsController | `api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations/* [GET]` | AuthPilot | WorkflowsExecute | P5-12 | Room-level load calculations execute resource-intensive logic and require staged permission checks. | Uses room-to-building scope fallback via reusable authorization gate. |
| BuildingCoolingReportsController | `api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling` | AuthPlanned | ReportsRead | P5-04B | Report data may expose engineering context without principal scoping. | Same staged policy applies to heating and energy-balance report controllers. |
| DevelopmentDemoDataController | `api/v{version:apiVersion}/development/demo-data` | AuthPilot | AdministrationManage | P5-09 | Development/demo data route exposure in production. | Pilot protection is controlled by `ApiAuthorization:EnableEndpointProtectionPilot`; must stay environment-gated (`IsDevelopment`). |
| StandardTablesController | `api/v{version:apiVersion}/standard-tables` | PublicAllowed | AnonymousAllowed / ProjectsRead | P5-04E | Reference data can remain public but must be explicitly documented. | Keep status explicit in inventory to avoid accidental policy drift. |

Machine-readable canonical inventory is maintained in `docs/security/api-endpoint-protection-inventory.json`.
