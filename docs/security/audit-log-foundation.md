# Audit Log Foundation

## Purpose

This document defines the foundation for security and engineering audit logging in AssistantEngineer without changing public API contracts or calculation physics.

## Scope

Audit foundation scope covers:

- authentication actions;
- authorization decisions;
- project/building/workflow actions;
- calculation execution actions;
- report and artifact access actions;
- principal and resource identifiers;
- metadata safety and bounded records;
- governance and registry policy.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No SIEM integration claim.

## Audit event model

Audit records are append-only logical entries represented by:

- `auditEventId`
- `occurredAtUtc`
- `eventType`
- `category`
- `outcome`
- principal identifiers (`userId`, `organizationId`, `externalSubjectId`)
- resource identifiers (`resourceType`, `resourceId`, `projectId`, `buildingId`, `workflowId`, `jobId`, `artifactId`)
- correlation fields (`correlationId`, `requestId`)
- bounded metadata dictionary.

## Event categories

- Authentication
- Authorization
- Project
- Building
- Workflow
- Calculation
- Report
- Artifact
- Administration
- Security
- System

## Outcome model

Allowed outcomes:

- `Succeeded`
- `Failed`
- `Denied`
- `Skipped`
- `Unknown`

## Principal/resource identifiers

- Audit records should include available principal and tenant identifiers from authentication and scoping boundaries.
- Resource references should use stable ids and type labels.
- Correlation identifiers should be carried when available.

## Metadata sanitization policy

- Metadata keys are sanitized case-insensitively.
- Forbidden keys are removed: `apiKey`, `x-api-key`, `token`, `access_token`, `refresh_token`, `password`, `secret`, `authorization`, `cookie`, `set-cookie`.
- Metadata values are truncated to bounded length (default: 512 characters).
- Null or empty metadata is permitted.

## Secret handling policy

- Never store API keys, tokens, passwords, secrets, cookies, or raw authorization headers.
- Never store full request/response payloads.
- Never store full large engineering artifacts in audit metadata.
- Store references (`artifactId`, checksum, summary metadata) instead of content blobs.

## Append-only policy

- Audit writes append new records and do not mutate historical records in place.
- Foundation writer behavior is append-only in memory.
- Future durable implementation must preserve append-only semantics and traceability.

## Durable storage status

- Current P5-05 scope provides in-memory writer and contracts.
- Durable DB-backed writer is intentionally deferred to a later targeted step.
- This step does not introduce schema migration for audit storage.

## Relationship to authentication boundary

- Authentication boundary (`docs/security/api-authentication-boundary.md`) may emit success/failure audit events without exposing credentials.
- Audit write failures must not alter authentication decision outcomes.

## Relationship to authorization policy

- Authorization rollout (`docs/security/authorization-policy-rollout.md`) may emit `Denied` and `Succeeded` audit events.
- Authorization failure responses should not disclose tenant mismatch details in audit metadata beyond safe codes.
- P5-12 execution rollout (`docs/security/protected-execution-endpoints-rollout.md`) keeps explicit execution authorization/start audit emission as staged future work to avoid coupling regressions.
- P5-13 report/artifact rollout (`docs/security/protected-report-artifact-endpoints-rollout.md`) keeps explicit report/artifact authorization event emission as staged future work in order to preserve low-risk rollout behavior.
- P5-14 workflow read/history rollout (`docs/security/protected-workflow-read-history-rollout.md`) keeps explicit workflow-read denial audit emission staged as future work to avoid regression risk while route guardrails stabilize.

## Relationship to observability

- Observability logs (`docs/architecture/observability-diagnostics-policy.md`) and audit logs are related but distinct.
- Observability targets operational telemetry; audit targets security and action traceability.
- Audit logs are not the source of calculation truth and must not influence physics outputs.

## Future work

- Add durable audit writer/provider with bounded storage and index strategy.
- Add retention and archival policy.
- Add query endpoints/pagination for privileged operators (future step).
- Add tenant-aware audit access policies.
- Add optional SIEM forwarding integration after security boundary rollout stabilizes.
- Add explicit audit triggers for repeated rate-limit violations (`docs/security/rate-limiting-foundation.md`).

Audit sanitization regression guardrails are tracked in `docs/security/security-regression-guardrails.md`.
