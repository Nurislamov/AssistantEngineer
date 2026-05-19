# Ownership Backfill Production Apply Enablement Proposal

## Purpose

This document defines a formal proposal for a future staged enablement of ownership-backfill apply in controlled staging and production environments. It does not enable apply execution in P6-09.

## Scope

This proposal covers:

- staging apply proposal prerequisites;
- production apply proposal prerequisites;
- approval policy and separation of duties;
- required evidence chain and `ApplyInputHash` linkage;
- backup readiness requirements;
- rollback readiness requirements;
- go/no-go criteria;
- change-management template requirements;
- explicit disabled current status.

## Non-claims

- No ownership backfill execution claim.
- No production apply enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Current status

- The CLI `apply` command exists only as a disabled stub and returns non-zero.
- The test-only executor exists for rehearsal only and is not a production apply path.
- No production apply executor is enabled.
- No production DB writes are possible through the current CLI.
- Passed readiness validation is necessary but not sufficient for enabling real apply.

## Required evidence chain

Future apply enablement requires the complete chain:

- dry-run summary artifact;
- evidence validation gate passed artifact;
- deterministic apply plan artifact;
- deterministic `PlanHash`;
- plan sign-off artifact;
- apply-readiness gate passed artifact;
- deterministic `ApplyInputHash`;
- previous-values completeness evidence;
- rollback-readiness pass evidence;
- signed change request referencing the same hash chain.

## Approval policy

Roles:

- Engineering owner;
- Security/review owner;
- Database/release owner;
- Business/application owner.

Policy rules:

- at least two independent approvals are required;
- production proposal requires staging rehearsal evidence first;
- the same person must not be both sole preparer and sole approver;
- every approval must explicitly reference `ApplyInputHash`;
- approvals expire after configured TTL and must be renewed if stale.

## Environment separation

- Local/Dev: dry-run and test-only rehearsal only.
- Staging: first candidate for real apply only after proposal acceptance in later stages.
- Production: only after staging success evidence and explicit re-approval.

Rules:

- staging and production evidence chains must be separate;
- connection strings must not be stored in evidence artifacts;
- `ApplyInputHash` is environment-specific when evidence differs;
- no production apply may run from an unmanaged local developer machine.

## Backup readiness

Before any future real apply:

- database backup is completed;
- restore procedure is verified;
- backup timestamp is recorded;
- backup owner is recorded;
- restore test evidence is linked in change records;
- no apply is permitted without backup confirmation.

## Rollback readiness

Before any future real apply:

- previous-values snapshot completeness is validated;
- rollback dry-run feasibility is confirmed;
- rollback owner is identified;
- rollback time window is estimated;
- rollback command remains a future separate stage;
- destructive rollback is not allowed.

## Go/no-go criteria

Go criteria:

- apply-readiness gate passed;
- approved `ApplyInputHash`;
- non-expired plan sign-off;
- unresolved rate below policy threshold;
- ambiguous records are zero or explicitly accepted by policy;
- previous-values completeness is sufficient;
- staging run evidence is successful for production proposal;
- backup and restore readiness is verified;
- release window is approved.

No-go criteria:

- hash mismatch across evidence chain;
- expired sign-off;
- missing previous-values snapshots;
- unresolved rate above threshold;
- unresolved ambiguous ownership without explicit policy acceptance;
- missing backup confirmation;
- restore verification missing;
- migration/version mismatch;
- pending migrations not explicitly accounted for;
- failed security regression tests;
- failed tenant isolation matrix tests.

## Change-management template

Required change-management fields:

- `ChangeId`
- `Environment`
- `RequestedBy`
- `ReviewedBy`
- `ApprovedBy`
- `ApplyInputHash`
- `PlanHash`
- `SignoffId`
- `ReadinessId`
- `DryRunRunId`
- `GateResultId`
- `BackupReference`
- `RollbackOwner`
- `ScheduledWindow`
- `GoNoGoDecision`
- `Notes`

## Future enablement stages

- P6-10 staging apply enablement design.
- P6-11 staging apply executor, guarded.
- P6-12 staging post-run evidence contract and acceptance validator.
- P6-13 production apply enablement decision.
- P6-14 manual write-path enablement decision framework (still no code writes).

## Production proposal dependency notes

- production proposal requires accepted staging runbook evidence;
- production proposal requires accepted staging executor evidence in a future enabled stage;
- production proposal requires accepted staging post-run acceptance result (`validate-staging-acceptance`) in a future enabled stage;
- production proposal requires a separate production promotion readiness decision from `ownership-backfill-production-promotion-readiness.md`;
- production proposal requires a human-approved manual decision log from `ownership-backfill-manual-decision-log-template.md` before any code-level enablement stage;
- production proposal requires accepted staging acceptance result before any production enablement decision;
- production `ApplyInputHash` is separate from staging `ApplyInputHash` when evidence differs;
- staging success is necessary but not sufficient for production enablement.
