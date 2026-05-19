# Ownership Backfill Staging Apply Runbook

## Purpose

This document defines future staging apply runbook requirements for ownership backfill while apply remains disabled in P6-10.

## Scope

This runbook covers:

- staging-only execution flow;
- staging evidence chain;
- `ApplyInputHash` binding;
- staging operator identity policy;
- staging backup and restore readiness;
- staging rollback rehearsal requirements;
- staging acceptance criteria;
- failed staging response model;
- promotion criteria for production proposal.

## Non-claims

- No staging apply execution claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Current status

- Apply command remains disabled.
- No staging DB writes are enabled.
- No production DB writes are enabled.
- This runbook is design-only.
- Test-only apply rehearsal is not staging apply.
- Staging executor design is documented in `ownership-backfill-staging-apply-executor-design.md` and remains disabled.
- Staging post-run evidence contract is documented in `ownership-backfill-staging-post-run-evidence.md`, and acceptance validation is future no-write governance only.

## Staging environment requirements

- A dedicated staging database is required.
- Staging database must be separate from production.
- Staging connection string must never be stored in evidence artifacts.
- Staging backup must exist before future apply.
- Restore procedure must be verified.
- Staging schema/migrations must match expected version.
- Pending migrations are disallowed unless explicitly accepted by policy.
- Staging data snapshot/source must be recorded.

## Staging operator policy

- Operator must be explicitly named.
- Operator must not be sole approver.
- Operator must have staging-only credentials.
- Operator must not use production credentials.
- Operator must record command transcript without secrets.
- Operator must reference `ApplyInputHash`.

## Required staging evidence chain

- dry-run summary;
- evidence validation gate result;
- apply plan;
- `PlanHash`;
- plan sign-off;
- readiness gate result;
- `ApplyInputHash`;
- previous-values snapshot;
- staging backup reference;
- rollback rehearsal plan;
- change request id.

## Staging command sequence, future only

Future staged sequence:

1. `dry-run` against staging.
2. `validate-evidence`.
3. `plan-apply`.
4. `signoff-plan`.
5. `validate-apply-readiness`.
6. future `apply` (disabled today).
7. post-apply dry-run.
8. post-apply tenant isolation matrix verification.
9. post-apply rollback readiness verification.
10. `validate-staging-acceptance` over post-run artifacts (future no-write acceptance gate).

Important:

- Future apply command is not enabled in P6-10.
- Command examples must not include real connection strings.

## Staging acceptance criteria

Staging run can be accepted only if:

- readiness gate passed;
- `ApplyInputHash` matches change request;
- future apply result has zero failed records;
- post-apply dry-run unresolved rate remains below threshold;
- tenant isolation matrix passes;
- route/security regression tests pass;
- rollback evidence is complete;
- no unexpected DTO/API/runtime behavior changes are observed;
- no secret/payload leakage is present in artifacts/logs.

## Staging failure handling

If staging fails:

- stop immediately;
- do not promote to production;
- preserve evidence artifacts;
- classify failure:
  - evidence mismatch;
  - hash mismatch;
  - unresolved above threshold;
  - ambiguous records;
  - schema mismatch;
  - rollback readiness failure;
  - test regression;
- create remediation task;
- rerun from dry-run after remediation.

## Promotion to production proposal

Production proposal can be considered only after:

- staging accepted;
- staging evidence archived;
- production dry-run evidence exists separately;
- production change request references production `ApplyInputHash`;
- approvals are renewed for production scope.
- production promotion readiness decision is validated via `ownership-backfill-production-promotion-readiness.md`.
