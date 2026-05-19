# Ownership Backfill Staging Post-run Evidence Contract

## Purpose

This document defines the future staging post-run evidence contract and acceptance validator model while staging apply remains disabled in P6-12.

## Scope

This contract covers:

- staging apply result artifact;
- post-apply dry-run evidence;
- post-apply evidence validation gate result;
- tenant isolation matrix reference;
- regression test reference;
- rollback evidence reference;
- deterministic `StagingRunHash`;
- acceptance/rejection decision model.

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

- staging apply remains disabled;
- post-run evidence is contract-only in this stage;
- no staging DB writes are enabled;
- no production DB writes are enabled.

## Required post-run evidence

A future accepted staging run must include:

- staging apply result;
- pre-apply `ApplyInputHash`;
- `PlanHash`;
- `SignoffId`;
- `ReadinessId`;
- staging preflight result/reference;
- previous-values snapshot reference;
- post-apply dry-run summary;
- post-apply evidence validation gate result;
- tenant isolation matrix result/reference;
- route/security regression result/reference;
- rollback readiness reference;
- operator id;
- staging change id.

## StagingRunHash model

`StagingRunHash` must be computed from normalized:

- `ApplyInputHash`;
- `PlanHash`;
- `SignoffId`;
- `ReadinessId`;
- staging preflight result/reference;
- staging apply result content;
- post-apply dry-run summary content;
- post-apply gate result content;
- rollback evidence reference;
- tenant isolation matrix reference;
- regression test reference;
- ruleset version.

Do not include:

- output paths;
- local machine directories;
- connection strings;
- secrets/tokens/payload fields.

## Acceptance criteria

Accepted only if:

- staging apply result status is `Succeeded`;
- failed records count is `0`;
- post-apply unresolved rate is at or below policy threshold;
- ambiguous ownership count is `0` unless policy explicitly allows exceptions;
- post-apply gate result is passed;
- tenant isolation matrix reference is present (and passed when structured status is available);
- regression reference is present (and passed when structured status is available);
- rollback evidence reference is complete when required;
- previous-values coverage is complete where policy requires it;
- `ApplyInputHash` matches the change request chain;
- sign-off is not expired at execution time;
- no secret/payload leakage is detected.

## Rejection criteria

Reject if:

- apply result artifact is missing;
- `StagingRunHash` is inconsistent for the same artifact chain;
- `ApplyInputHash` mismatch is detected;
- post-apply unresolved drift exceeds threshold;
- failed records count is greater than `0`;
- tenant isolation matrix fails (when structured status is available and required);
- regression checks fail (when structured status is available and required);
- rollback evidence is incomplete;
- schema/version mismatch is detected;
- secret/payload leakage is detected;
- operator id or staging change id is missing.

## Failure taxonomy

Failure categories:

- `EvidenceMismatch`
- `HashMismatch`
- `ApplyFailure`
- `UnresolvedDrift`
- `AmbiguousOwnership`
- `RollbackEvidenceMissing`
- `RegressionFailure`
- `TenantIsolationFailure`
- `SchemaMismatch`
- `SecretLeakage`
- `MissingApproval`
- `Unknown`

## Future production promotion

Production promotion requires:

- staging acceptance result is `Accepted`;
- staging evidence is archived;
- production dry-run evidence is separate;
- production `ApplyInputHash` is separate when evidence differs;
- approvals are renewed for production scope.
- production promotion readiness decision is validated with `ownership-backfill-production-promotion-readiness.md`.
