# Ownership Backfill Test-Only Apply Rehearsal

## Purpose

This document defines the P6-07 test-only apply executor rehearsal for ownership backfill plans. It validates executor behavior without enabling production apply writes.

## Scope

P6-07 scope includes:

- test-only executor contracts and execution model;
- simulated batch execution behavior in controlled test stores;
- idempotency and conflict-handling rehearsal coverage;
- previous-values capture verification;
- rehearsal result artifact generation for tests and governance.

## Non-claims

- No ownership backfill execution claim.
- No production apply enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Test-only executor model

P6-07 introduces `TestOnlyOwnershipBackfillApplyExecutor` with explicit test-only request contracts:

- `IOwnershipBackfillApplyExecutor`
- `OwnershipBackfillApplyExecutionRequest`
- `OwnershipBackfillApplyExecutionResult`
- `OwnershipBackfillApplyExecutionFinding`
- `IOwnershipBackfillTestRecordStore`
- `InMemoryOwnershipBackfillTestRecordStore`

The executor can only run when `TestOnlyExecution=true` and provider is explicitly test-only (`InMemory` or `SQLiteTemp`).

## What is not connected to CLI apply

- CLI `apply` remains disabled and non-zero.
- CLI `apply` does not invoke rehearsal executor paths.
- user-provided production/real DB connection strings are not used for rehearsal execution.
- no production DB write path is enabled.

## Batch simulation

Rehearsal execution simulates batch policy with configured `BatchSize`:

- planned records are processed in deterministic order;
- records are chunked by batch size;
- failures are tracked per-record;
- failed records are not updated.

## Idempotency behavior

Rehearsal executor is idempotent for identical plan/test-store input:

- records that already match proposed ownership are skipped;
- repeated execution after successful update does not re-update matching records.

## Conflict handling

Rehearsal executor skips records on controlled conflict cases:

- current values conflict with planned expected/proposed values;
- ambiguous planned reasons;
- unresolved planned records with missing proposed ownership identifiers;
- planned-record deterministic hash mismatch.

## Previous-values rehearsal

Before simulated update, executor captures previous values for updated records:

- `recordType`
- `recordId`
- `previousProjectId`
- `previousBuildingId`
- `previousOrganizationId`
- `previousOwnerUserId`

No payload data or secrets are captured.

## Generated rehearsal artifacts

`OwnershipBackfillApplyExecutionResultWriter` writes run-scoped rehearsal artifacts:

- `ownership-backfill-apply-rehearsal-result-{executionId}.json`
- `ownership-backfill-apply-rehearsal-result-{executionId}.md`
- `ownership-backfill-rehearsal-previous-values-{executionId}.json`

These artifacts are test/generated outputs and must not be committed.

## Safety guarantees

- test-only execution flag is mandatory;
- production providers are refused;
- no runtime API controller behavior is changed;
- no production service/DbContext write path is enabled;
- no `SaveChanges`/`SaveChangesAsync` path is introduced in this tooling stage.

## Known limitations

- this is not a production apply executor;
- no real DB apply path is enabled;
- no ownership backfill execution is performed;
- test-only rehearsal pass is not equivalent to apply-readiness gate pass;
- apply CLI remains disabled until a later explicit stage.

## Next steps

- P6-08: define real apply enablement readiness checklist while still disabled.
- P6-09+: evaluate controlled enablement gates for non-production rehearsal environments before any production write path discussion.
