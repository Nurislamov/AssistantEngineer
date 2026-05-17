# Protected Report/Artifact Endpoints Rollout

## Purpose

This document records P5-13 controlled rollout of report/artifact endpoint protection without broad global route lock-down.

## Scope

This rollout covers:

- report read/view endpoints in report controllers;
- report export endpoints in report controllers;
- workflow report and report-export endpoints (`/engineering-workflow/report`, `/report/export/*`);
- workflow trace-preview endpoint (`/engineering-workflow/trace-preview`);
- workflow artifact read endpoints (`/engineering-workflow/scenarios/{scenarioId}/artifacts*`).

This rollout does not introduce new public artifact endpoints.
Workflow read/history route protection is handled separately in P5-14 (`docs/security/protected-workflow-read-history-rollout.md`).

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.
- No claim that artifact ownership is fully enforced unless descriptor scope exists.

## Selected endpoint groups

- `BuildingCoolingReportsController`
- `BuildingHeatingReportsController`
- `BuildingEnergyBalanceReportsController`
- `EngineeringWorkflowController` report/trace/artifact-read endpoints.

## Report read policy

- Required permission: `ReportsRead`.
- Applied to report read/view and export routes backed by building scope.
- Enforced only when:
  - `ApiAuthorization:Enabled=true`;
  - `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot=true`;
  - `ApiAuthorization:RequireReportReadAuthorization=true`.

## Report generate/export policy

- Required permission: `ReportsWrite` for workflow report generation/export endpoints.
- Applied to `POST /engineering-workflow/report` and `POST /engineering-workflow/report/export/*`.
- Enforced only when:
  - `ApiAuthorization:Enabled=true`;
  - `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot=true`;
  - `ApiAuthorization:RequireReportWriteAuthorization=true`.

## Artifact read/write policy

- Artifact read uses `ReportsRead` in this stage.
- Dedicated `ArtifactRead/ArtifactWrite` permissions are not introduced in P5-13; `ReportsRead/ReportsWrite` are used as staged policy mapping.
- Public artifact write/delete API endpoints are not exposed in current API controller surface, so write/delete protection remains deferred until such endpoints exist.
- Artifact-read enforcement is controlled by `RequireArtifactReadAuthorization` under the report/artifact pilot flag.

## Resource scope resolution

- Report endpoints use building scope (`buildingId`) and workflow request state scope (`projectId`, `buildingId`) when available.
- Artifact-read endpoints attempt workflow scope by `scenarioId` and fall back to available project/building scope when present.
- When workflow/artifact ownership descriptors are unavailable, permission-only fallback is used and this limitation is explicitly documented.

## Compatibility defaults

Compatibility-safe defaults remain:

- `ApiAuthorization:Enabled=false`;
- `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot=false`;
- `ApiAuthorization:RequireReportReadAuthorization=false`;
- `ApiAuthorization:RequireReportWriteAuthorization=false`;
- `ApiAuthorization:RequireArtifactReadAuthorization=false`;
- `ApiAuthorization:RequireArtifactWriteAuthorization=false`;
- `ApiAuthorization:AllowAnonymousInDevelopment=true`.

## Tenant mismatch behavior

- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false` => mismatch returns `403`.
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=true` => mismatch returns `404`.
- Responses do not disclose tenant mismatch details.

## Rate limiting relationship

- P5-13 aligns with `ReportGenerate`, `ArtifactRead`, and `ArtifactWrite` rate-limit categories in `docs/security/rate-limiting-policy-registry.json`.
- This rollout does not enable global rate limiting by default.

## Audit/observability behavior

- Authorization-denied decisions continue to use structured logs from the authorization gate without payload/secret logging.
- Explicit report/artifact authorization audit event emission is deferred to a future stage to avoid fragile coupling.
- Audit failures therefore cannot alter endpoint results in this rollout step.

## Test matrix

- Pilot disabled preserves existing behavior.
- Report generate protection enabled + no credentials => `401`.
- Report generate protection enabled + missing `ReportsWrite` => `403`.
- Report generate protection enabled + matching `ReportsWrite` and scope => success.
- Report read endpoints require `ReportsRead`.
- Tenant mismatch respects `403/404` option.
- Artifact read endpoints require `ReportsRead` under pilot options.
- Existing project/building and execution pilots remain unchanged.

## Public artifact endpoint status

- Public artifact read endpoints exist in `EngineeringWorkflowController`.
- Public artifact write/delete endpoints are not currently exposed.
- Protection for artifact write/delete endpoints is deferred until endpoint exposure exists.

## What remains unprotected

- Dedicated artifact write/delete API route enforcement (no public endpoints yet).
- Workflow read/history group is handled in separate rollout (`docs/security/protected-workflow-read-history-rollout.md`).
- Complete tenant-boundary enforcement at query/persistence layers.
- Full workflow/artifact ownership descriptor coverage for anti-enumeration behavior.

## Next rollout candidates

- Workflow read/history endpoints (`WorkflowsRead`) with explicit ownership-scope checks.
- Dedicated artifact write/delete endpoint protection if/when API exposure is introduced.
- Deeper tenant-isolation integration tests across report/artifact and workflow history paths.
- Optional durable audit emission for report/artifact authorization events.

P5-15 tenant isolation note:

- Cross-tenant expectations for `ReportsRead`, `ReportsWrite`, and report-artifact read paths are tracked in `docs/security/tenant-isolation-integration-matrix.md`.
- Dedicated public artifact write/delete endpoints remain deferred until such endpoints exist.
