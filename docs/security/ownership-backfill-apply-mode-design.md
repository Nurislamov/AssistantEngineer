# Ownership Backfill Apply Mode Design

## Purpose

This document defines the future apply-mode design and safety contract for ownership backfill. It does not enable execution in P6-04.

## Scope

This design covers:

- required dry-run evidence;
- required validation gate result;
- apply command contract;
- batch execution model;
- previous-values snapshot requirements;
- rollback evidence requirements;
- idempotency rules;
- failure handling;
- audit/observability expectations;
- explicit disabled status.

## Non-claims

- No ownership backfill execution claim.
- No apply mode enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Apply preconditions

Future apply mode must require all of the following:

- explicit `apply` command;
- explicit `--enable-apply` flag;
- explicit `--evidence <dry-run-directory>`;
- explicit `--gate-result <gate-result-json>`;
- gate result `Passed=true`;
- dry-run summary has `Mode=DryRun`;
- unresolved thresholds are passed;
- ambiguous records count is less than or equal to threshold;
- approved `plan-apply` artifacts exist with deterministic `PlanHash`;
- approved `signoff-plan` artifact exists for the target plan;
- approved `validate-apply-readiness` result artifact exists for the same input chain;
- signed plan evidence is present and bound to the exact target `PlanHash`;
- signoff `PlanHash` exactly matches apply plan `PlanHash`;
- readiness `ApplyInputHash` matches apply input chain artifacts;
- signoff reviewer/ticket metadata is present and signoff is not expired;
- future apply evidence must reference a matching `PlanHash` and `RulesetVersion`;
- accepted production/staging enablement proposal exists with required approvals;
- change-management record references matching `ApplyInputHash`;
- previous-values snapshot output path is configured;
- database provider and connection string are explicitly supplied;
- migration/version compatibility check passes;
- `Project.OrganizationId` column exists;
- no unexpected pending migrations unless explicitly allowed by policy;
- exact confirmation phrase is supplied.

## Confirmation phrase

Required exact phrase:

`I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA`

Rules:

- non-interactive usage must pass the phrase via CLI argument;
- CI should not rely on interactive prompts unless explicitly allowed in future;
- missing or incorrect confirmation phrase refuses apply.

## Apply command contract

Future command shape:

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply \
  --evidence <dry-run-dir> \
  --gate-result <gate-result-json> \
  --plan <plan-json> \
  --plan-signoff <signoff-json> \
  --output <apply-output-dir> \
  --database-provider PostgreSQL \
  --connection-string "<explicit>" \
  --enable-apply \
  --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA
```

P6-04 behavior:

- command always returns non-zero;
- command always reports apply is designed but disabled;
- no database write operations are executed.

## Batch policy

- default batch size `500`;
- transaction boundary per batch;
- idempotent updates only;
- records already matching target ownership are skipped;
- ambiguous records are skipped;
- unresolved records are skipped;
- stop-on-first-batch-error is default;
- apply summary output is required in future enabled mode.

## Previous-values snapshot

Before any update in future enabled mode, snapshot must include:

- `recordType`
- `recordId`
- `previousProjectId`
- `previousBuildingId`
- `previousOrganizationId`
- `previousOwnerUserId`
- `proposedProjectId`
- `proposedBuildingId`
- `proposedOrganizationId`
- `proposedOwnerUserId`

Payloads are forbidden.

## Rollback design

- rollback is a future separate command;
- apply mode must emit enough previous-values evidence for rollback planning;
- destructive rollback is not allowed;
- rollback must have its own governance gates.

## Idempotency model

- identity key: record identity plus proposed ownership hash;
- repeated apply with same evidence should be no-op for already converged records;
- conflicting current values are skipped and reported.
- approved plan hash from P6-05 must match the apply input set.

## Audit/observability

Future apply observability should include:

- start/completed/failed event boundaries;
- no payload logging;
- no connection-string or secret logging;
- runId, batch number, and count summaries.

## Disabled status

- P6-04 does not enable apply mode.
- no DB write code is active for apply.
- no `SaveChanges`/`SaveChangesAsync` is used in apply command path.
- P6-05 adds `plan-apply` evidence planning only; apply remains disabled.
- P6-06 adds `signoff-plan` governance only; apply remains disabled.
- P6-07 adds test-only executor rehearsal only; apply remains disabled.
- P6-08 adds apply-readiness governance gate and ApplyInputHash validation only; apply remains disabled.
- P6-09 adds production/staging enablement proposal and change-management template requirements; apply remains disabled.
- P6-10 requires staging-first enablement design and accepted staging runbook evidence before any production enablement discussion.
- P6-11 adds staging executor contract and preflight design only; staging apply remains disabled.
- P6-14 adds manual write-path enablement decision artifacts only; this manual decision is necessary but not sufficient for code enablement.
- P6-15 adds architecture review invariants and source-level no-wiring governance; apply remains disabled.
- Future apply cannot be enabled by code alone; policy approval and change-management acceptance are mandatory.
- Staging apply remains disabled in P6-10.
- Production apply cannot be enabled before accepted staging evidence exists.
