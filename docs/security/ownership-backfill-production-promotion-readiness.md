# Ownership Backfill Production Promotion Readiness

## Purpose

This document defines production promotion readiness requirements after accepted staging evidence, while production apply remains disabled in P6-13.

## Scope

This proposal covers:

- staging acceptance as a hard prerequisite;
- separate production evidence chain;
- production `ApplyInputHash` requirements;
- cross-environment separation validation;
- TTL and re-approval policy;
- production change request binding;
- production backup and rollback readiness;
- production promotion decision artifact contract;
- explicit disabled current status.

## Non-claims

- No production apply enabled claim.
- No staging apply execution claim.
- No production ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Current status

- Staging acceptance validator exists and produces no-write acceptance artifacts.
- Production apply remains disabled.
- Staging acceptance is necessary but not sufficient for production promotion.
- Production evidence chain must be separate from staging evidence chain.
- No production DB writes are enabled.

## Required staging evidence

Production promotion requires the following staging evidence:

- staging acceptance result with `Accepted=true`;
- `StagingRunHash`;
- staging `ApplyInputHash`;
- staging change id;
- staging operator id;
- staging evidence archive reference;
- staging signoff not expired at staging execution time;
- staging failed records equal `0`;
- staging rollback evidence complete.

## Required production evidence chain

Production promotion also requires a separate production evidence chain:

- production dry-run summary;
- production evidence gate passed;
- production apply plan;
- production `PlanHash`;
- production signoff;
- production readiness gate passed;
- production `ApplyInputHash`;
- production previous-values completeness;
- production backup and restore readiness;
- production rollback readiness;
- production change request id.

## Cross-environment separation rules

- Staging `ApplyInputHash` must not be reused as production `ApplyInputHash` by default.
- Production dry-run evidence must come from production dataset.
- Production signoff must be separate from staging signoff.
- Production readiness result must be separate from staging readiness result.
- Production backup reference must be production-specific.
- Production change request id must be production-specific.
- No staging connection string/reference may appear in production artifacts.
- No production connection string may appear in any evidence artifact.

## TTL and re-approval policy

- Staging acceptance expires after configured TTL (default 72h) unless refreshed.
- Production signoff expires after configured TTL (default 24h) unless refreshed.
- Any artifact content change requires re-approval.
- If production dry-run changes, regenerate plan/signoff/readiness.
- If schema or migration baseline changes, rerun the full production evidence chain.
- Expired staging acceptance blocks production promotion readiness.

## Production promotion decision

Decision statuses:

- `NotReady`
- `ReadyForProductionApproval`
- `Rejected`
- `Expired`

P6-13 does not enable production apply and does not execute ownership backfill.

This readiness decision feeds the manual write-path enablement decision framework documented in `ownership-backfill-manual-write-path-enablement-decision.md`.
Production promotion readiness is also not sufficient for code enablement without the P6-15 architecture review acceptance (`ownership-backfill-apply-enablement-architecture-review.md`).

## Go/no-go criteria

Go criteria:

- staging accepted;
- complete production evidence chain;
- approved production `ApplyInputHash`;
- production backup and restore verified;
- production previous-values completeness verified;
- production rollback readiness verified;
- tenant isolation matrix and regression references pass governance policy;
- no secret/payload leakage;
- approvals valid and non-expired.

No-go criteria:

- staging acceptance missing, expired, or rejected;
- production `ApplyInputHash` missing or mismatched;
- production unresolved rate above threshold;
- unresolved ambiguous ownership;
- backup missing;
- rollback evidence incomplete;
- regression failure;
- tenant isolation matrix failure;
- schema or migration mismatch;
- approvals missing or expired;
- any evidence contains secrets.
## CLI command inventory reference
See [ownership-backfill-cli-command-inventory.md](ownership-backfill-cli-command-inventory.md) for canonical command list, exit codes, and redaction policy.
