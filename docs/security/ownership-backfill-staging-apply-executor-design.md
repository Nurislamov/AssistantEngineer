# Ownership Backfill Staging Apply Executor Design

## Purpose

This document defines the future staging-only apply executor design while execution remains disabled in P6-11.

## Scope

This design covers:

- staging-only executor contract;
- environment guard model;
- preflight checks;
- schema/version gate;
- backup/restore gate;
- hash-chain/readiness/signoff gate;
- batch execution design;
- deterministic post-run evidence contract;
- rollback evidence requirements;
- disabled current status.

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

- staging apply remains disabled.
- production apply remains disabled.
- this step adds contracts and design only.
- no DB writes are enabled.
- test-only rehearsal is not staging apply.

## Staging-only executor contract

Future executor must require:

- `environment = Staging`;
- explicit staging enablement flag;
- approved staging runbook/checklist evidence;
- `ApplyInputHash`;
- signed plan;
- passed readiness result;
- backup reference;
- rollback readiness reference;
- staging-only credentials;
- no production provider/connection.

## Environment guard

Rules:

- environment must be explicitly declared as `Staging`;
- `Production` must be hard-denied;
- unknown environment must be hard-denied;
- connection string must not be used for environment inference;
- connection string must not be logged;
- environment validation uses explicit allowlist;
- optional database metadata staging marker can be added in future.

## Preflight checks

Required before any future staging write:

- apply remains disabled unless future stage enables it;
- dry-run evidence exists;
- evidence gate passed;
- plan generated;
- signoff valid and non-expired;
- readiness gate passed;
- `ApplyInputHash` matches all artifacts;
- schema/version gate passed;
- backup/restore gate passed;
- rollback evidence complete;
- no pending migrations unless explicitly approved;
- operator identity valid;
- transcript redaction enabled.

## Schema/version gate

Future staging apply must verify:

- expected migration set;
- `Project.OrganizationId` column exists;
- `Project.OwnerUserId` column exists;
- workflow/scenario/job metadata coverage baseline matches P5-17 expectations;
- no unknown pending migrations unless approved.

## Backup/restore gate

Future staging apply must verify:

- backup completed;
- restore rehearsal verified;
- backup owner recorded;
- restore owner recorded;
- backup reference linked in runbook/checklist;
- backup timestamp is before apply window.

## Batch execution design

- default batch size `500`;
- transaction boundary per batch;
- idempotent updates;
- current-value conflicts skipped;
- ambiguous/unresolved skipped;
- stop-on-batch-error by default;
- post-run summary required;
- previous-values snapshot captured before updates;
- no payload/secret fields in artifacts.

## Post-run evidence contract

Future staging apply must generate:

- staging apply result (`json`/`md`);
- staging post-apply dry-run summary;
- staging tenant-isolation test reference;
- staging regression-test reference;
- updated unresolved metrics;
- changed records count;
- skipped records count;
- failed records count;
- rollback evidence reference.

Reference contract:

- `docs/security/ownership-backfill-staging-post-run-evidence.md`
- `validate-staging-acceptance` performs deterministic post-run acceptance validation in no-write mode.

## Disabled status

- no staging apply command is enabled in P6-11;
- CLI `apply` remains disabled;
- no `SaveChanges` in staging executor path;
- no real DB writes.
## CLI command inventory reference
See [ownership-backfill-cli-command-inventory.md](ownership-backfill-cli-command-inventory.md) for canonical command list, exit codes, and redaction policy.
