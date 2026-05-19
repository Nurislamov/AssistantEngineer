# Ownership Backfill Manual Write-path Enablement Decision

## Purpose

This document defines a human-only decision framework for any future ownership-backfill write-path enablement, while code apply remains disabled.

## Scope

This framework covers:

- manual enablement decision packet;
- required approval packet;
- ProductionPromotionHash binding;
- ApplyInputHash binding;
- go/no-go decision;
- TTL and expiry verification;
- sign-off chain verification;
- risk acceptance;
- rollback ownership;
- explicit code-disabled current status.

## Non-claims

- No production apply enabled claim.
- No staging apply execution claim.
- No production ownership backfill execution claim.
- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Current status

- CLI `apply` remains disabled.
- No production write path is enabled.
- No staging write path is enabled.
- This step adds manual decision artifacts only.
- Production promotion readiness is necessary but not sufficient.
- Human approval cannot bypass code-disabled state.

## Required decision packet

A manual decision packet must include:

- `ProductionPromotionHash`;
- production `ApplyInputHash`;
- production `PlanHash`;
- `ProductionPromotionDecisionId`;
- `ProductionChangeRequestId`;
- `StagingRunHash`;
- staging acceptance reference;
- production readiness reference;
- production signoff reference;
- production previous-values reference;
- production backup reference;
- rollback owner;
- approvals;
- TTL/expiry verification;
- go/no-go decision;
- explicit risk acceptance.

## Manual approval policy

Roles:

- `EngineeringOwner`;
- `SecurityReviewer`;
- `DatabaseReleaseOwner`;
- `BusinessApplicationOwner`.

Rules:

- minimum three approvals are required for production write-path enablement;
- `DatabaseReleaseOwner` approval is mandatory;
- `SecurityReviewer` approval is mandatory;
- approver cannot be sole preparer;
- approvals must reference `ProductionPromotionHash` and `ApplyInputHash`;
- approvals expire after TTL;
- any evidence artifact change invalidates approvals.

## Go/no-go review checklist

Go requires all of the following:

- `validate-production-promotion` result is `Ready=true`;
- `ProductionPromotionHash` matches decision packet;
- `ApplyInputHash` matches decision packet;
- staging acceptance is still valid;
- production signoff is still valid;
- backup and restore are verified;
- previous-values are complete;
- rollback owner is confirmed;
- tenant isolation matrix is passed;
- route/security regression is passed;
- no secret/payload leakage is detected;
- release window is approved.

No-go applies if any of the following occur:

- any hash mismatch;
- expired signoff;
- expired staging acceptance;
- missing backup;
- missing rollback owner;
- incomplete previous-values;
- unresolved rate above threshold;
- ambiguous ownership unresolved;
- failed tests;
- evidence contains secrets;
- production/staging environment separation is violated.

## Code enablement boundary

- P6-14 does not enable code write path.
- P6-14 does not add production apply executor.
- P6-14 does not change CLI `apply` behavior.
- Any future code enablement requires a separate stage and separate review.
- Manual decision log is not executable.

## Human-only decision log

Decision log fields:

- `DecisionId`;
- `DecisionStatus` (`NotReady`, `Go`, `NoGo`, `Expired`, `Superseded`);
- `ProductionPromotionHash`;
- `ApplyInputHash`;
- `PlanHash`;
- `StagingRunHash`;
- `ProductionChangeRequestId`;
- approvers;
- approval timestamps;
- expiry;
- risk acceptance;
- rollback owner;
- notes;
- non-claims.

## Future code enablement boundary

Any future code stage must:

- consume manual decision log;
- require accepted P6-15 architecture review artifacts (`ownership-backfill-apply-enablement-architecture-review.md` and checklist template outputs);
- validate hashes;
- validate TTL;
- keep dry-run/readiness/gate validations;
- require explicit environment;
- enable staging before production if not already enabled;
- preserve rollback evidence requirements.
