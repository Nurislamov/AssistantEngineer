# Ownership Backfill Strategy

## Purpose

This document defines a safe staged ownership backfill strategy before stricter tenant persistence enforcement is considered. P6-00 is strategy-only and does not execute backfill updates.

Canonical release-boundary reference: `docs/security/security-release-boundary.md`.

## Scope

This strategy covers:

- Projects with null `OrganizationId` / `OwnerUserId`.
- Workflow state records.
- Scenario records.
- Job records.
- Job event records.
- Scenario history records.
- Report/artifact metadata where available.
- Evidence and governance requirements.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No ownership backfill execution claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Backfill principles

- Dry-run is mandatory before any write mode.
- Evidence artifacts are append-only.
- No destructive updates are allowed.
- Batch processing is required.
- Updates must be idempotent.
- Source-of-truth rules must be explicit and deterministic.
- Ambiguous records are skipped and reported, never guessed.
- Payloads/secrets/tokens are never logged.
- No automatic assignment to arbitrary organization.
- Global query filters are deferred until backfill coverage evidence is accepted.

## Source-of-truth hierarchy

Project ownership:

1. Existing `Project.OrganizationId` when already set.
2. Future manual/admin assignment.
3. Backfill from trusted project ownership mapping file, when provided.
4. Otherwise unresolved.

Building ownership:

1. Parent `Project.OrganizationId`.
2. Otherwise unresolved.

WorkflowState ownership:

1. `ProjectId` -> `Project.OrganizationId`.
2. `BuildingId` -> Building -> Project -> `OrganizationId`.
3. Otherwise unresolved.

Scenario ownership:

1. `Scenario.ProjectId` -> `Project.OrganizationId`.
2. `Scenario.BuildingId` -> Building -> Project -> `OrganizationId`.
3. `Scenario.WorkflowId` -> workflow state -> project/building ownership (when metadata path exists).
4. Otherwise unresolved.

Job ownership:

1. `Job.ScenarioId` -> Scenario ownership.
2. `Job.ProjectId` -> `Project.OrganizationId`.
3. `Job.BuildingId` (if present) -> Building -> Project ownership.
4. `Job.WorkflowId` (if present) -> workflow state ownership.
5. Otherwise unresolved.

JobEvent ownership:

1. `JobId` -> Job ownership.
2. `ScenarioId` -> Scenario ownership.
3. `ProjectId` -> Project ownership.
4. Otherwise unresolved.

ScenarioHistory ownership:

1. `ScenarioId` -> Scenario ownership.
2. `ProjectId` (if present) -> Project ownership.
3. Otherwise unresolved.

## Dry-run metrics

Required dry-run metrics:

- `totalProjects`
- `unscopedProjects`
- `scopedProjects`
- `unresolvedProjects`
- `totalWorkflowStates`
- `workflowStatesWithResolvableOwnership`
- `workflowStatesUnresolved`
- `totalScenarios`
- `scenariosWithResolvableOwnership`
- `scenariosUnresolved`
- `totalJobs`
- `jobsWithResolvableOwnership`
- `jobsUnresolved`
- `totalJobEvents`
- `jobEventsWithResolvableOwnership`
- `jobEventsUnresolved`
- `totalScenarioHistoryRecords`
- `scenarioHistoryWithResolvableOwnership`
- `scenarioHistoryUnresolved`
- `totalRecordsScanned`
- `totalRecordsResolvable`
- `totalRecordsUnresolved`
- `unresolvedByReason`

## Dry-run output model

Expected dry-run outputs:

- JSON summary.
- Markdown summary.
- Unresolved records JSON/CSV.
- No secrets and no raw payloads.
- No engineering calculation payloads.
- Output includes only identifiers, record type, unresolved reason, and candidate ownership IDs.

## Batch plan

- Default batch size: `500` or `1000`.
- Dry-run mode required first.
- Write mode disabled by default.
- Idempotent update rules are mandatory.
- Retry-safe behavior is required.
- Transaction boundary is per batch.
- Stop-on-error behavior is enabled by default for safety.

## Safety checks

- Backfill cannot run without explicit `--apply` (or `Apply=true`).
- Apply-mode design requires a previously generated dry-run evidence file and a successful `validate-evidence` gate result.
- Evidence gates fail when unresolved rates exceed configured thresholds.
- Evidence gates fail when ambiguous ownership is detected (default policy).
- Apply mode refuses execution when expected migration/version baseline is missing.
- Apply mode refuses execution when `Project.OrganizationId` column is missing.
- Logs are summary-only and never include payload content.

## Rollback notes

- Rollback scope is metadata-only.
- Before apply mode, export affected IDs and previous ownership values.
- Rollback tool/script is a future P6 step.
- Destructive rollback is not allowed.

## Governance gates

Before evaluating global query filters or DB RLS:

- Backfill dry-run evidence exists.
- Evidence-gate result exists and is passing.
- Unresolved rate is below approved threshold.
- Tenant isolation matrix tests pass.
- Protected route authorization tests pass.
- Security regression guardrails pass.
- No full tenant isolation claim is made until all gates pass.

## Future steps

- P6-01: dry-run tool skeleton.
- P6-02: read-only database dry-run scanner and fixtures.
- P6-03: evidence validation gates and unresolved-threshold policy.
- P6-04: apply-mode design (still disabled) with explicit safety switch.
- P6-05: no-write `plan-apply` generator from passed evidence with deterministic plan hash and apply-summary draft artifacts.
- P6-06: no-write `signoff-plan` governance gate with reviewer/ticket metadata and plan hash verification.
- P6-07: test-only apply executor rehearsal with no production writes.
- P6-08: real apply enablement readiness checklist with deterministic ApplyInputHash (still disabled).
- P6-09: production apply enablement proposal (still no code writes).
- P6-10: staging apply enablement design (still disabled).
- P6-11: staging apply executor design (still disabled).
- P6-12: staging post-run evidence contract and acceptance validator (still no writes).
- P6-13: production promotion readiness proposal.
- P6-14: manual real write-path enablement decision framework (still no code writes).
- P6-15: apply enablement architecture review (still no code enablement).
- P6-16: evaluate global query filters readiness.
- P6-17: evaluate DB row-level security readiness.
- P7-00: post-P6 governance audit and release boundary review.
- P7-01: security governance docs deduplication, canonical release-boundary definition, and status/index normalization.
- P7-02: governance test consolidation and shared helper extraction to reduce maintenance complexity while preserving no-write guardrails.

P6-01 reference:

- `docs/security/ownership-backfill-dry-run-tool.md`

P6-02 reference:

- `docs/security/ownership-backfill-database-dry-run-scanner.md`

P6-03 reference:

- `docs/security/ownership-backfill-evidence-validation-gates.md`

P6-04 reference:

- `docs/security/ownership-backfill-apply-mode-design.md`
- P6-04 keeps apply mode explicitly disabled.

P6-05 reference:

- `docs/security/ownership-backfill-apply-plan-generator.md`
- P6-05 keeps apply mode explicitly disabled and produces plan artifacts only.

P6-06 reference:

- `docs/security/ownership-backfill-plan-signoff-gate.md`
- P6-06 keeps apply mode explicitly disabled and produces sign-off artifacts only.

P6-07 reference:

- `docs/security/ownership-backfill-test-only-apply-rehearsal.md`
- P6-07 keeps production apply explicitly disabled and runs only controlled test-only rehearsal execution.

P6-08 reference:

- `docs/security/ownership-backfill-apply-enablement-readiness.md`
- P6-08 keeps production apply explicitly disabled and validates artifact-chain readiness only.

P6-09 reference:

- `docs/security/ownership-backfill-production-apply-enablement-proposal.md`
- `docs/security/ownership-backfill-change-request-template.md`
- P6-09 keeps production apply explicitly disabled and defines proposal/change-management policy only.

P6-10 reference:

- `docs/security/ownership-backfill-staging-apply-runbook.md`
- `docs/security/ownership-backfill-staging-acceptance-checklist.md`
- P6-10 keeps staging/production apply explicitly disabled and defines staging governance/runbook policy only.

P6-11 reference:

- `docs/security/ownership-backfill-staging-apply-executor-design.md`
- P6-11 keeps staging/production apply explicitly disabled and defines staging executor contract/preflight design only.

P6-12 reference:

- `docs/security/ownership-backfill-staging-post-run-evidence.md`
- P6-12 keeps staging/production apply explicitly disabled and validates post-run acceptance artifacts in no-write mode only.

P6-13 reference:

- `docs/security/ownership-backfill-production-promotion-readiness.md`
- P6-13 keeps staging/production apply explicitly disabled and validates production promotion readiness artifacts in no-write mode only.

P6-14 reference:

- `docs/security/ownership-backfill-manual-write-path-enablement-decision.md`
- `docs/security/ownership-backfill-manual-decision-log-template.md`
- P6-14 keeps staging/production apply explicitly disabled and defines human-only decision packet/checklist artifacts only.

P6-15 reference:

- `docs/security/ownership-backfill-apply-enablement-architecture-review.md`
- `docs/security/ownership-backfill-architecture-review-checklist.md`
- P6-15 keeps staging/production apply explicitly disabled and formalizes no-wiring/no-secrets/no-destructive-sql invariants before any future code enablement discussion.

P7-00 reference:

- `docs/security/post-p6-governance-audit.md`
- `docs/security/security-governance-index.md`
- P7-00 audits P5/P6 governance claims, release boundary, and disabled apply posture without enabling any write path.

P7-04 reference:

- docs/security/release-ready-observability-audit.md`r
- P7-04 improves release-ready observability diagnostics and timing visibility without changing release-gate semantics or enabling any write path.
