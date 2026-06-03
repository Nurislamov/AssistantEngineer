# Ownership Backfill Plan Signoff Gate

## Purpose

This document defines the P6-06 sign-off gate for ownership backfill apply-plan artifacts. It records how a plan is reviewed and approved without enabling any ownership writes.

## Scope

P6-06 scope includes:

- plan artifact validation;
- deterministic `PlanHash` verification;
- reviewer/ticket metadata requirements;
- sign-off artifact generation;
- expiration and governance rules;
- apply-precondition linkage.

## Non-claims

- No ownership backfill execution claim.
- No apply mode enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Command usage

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- signoff-plan \
  --plan <ownership-backfill-apply-plan-{planId}.json> \
  --expected-plan-hash <hash> \
  --reviewer <name-or-id> \
  --ticket <ticket-or-change-id> \
  --output <signoff-output-dir> \
  --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN
```

Optional:

- `--notes <text>`
- `--expires-at <utc timestamp>`
- `--force-overwrite <true|false>`

## Required inputs

`signoff-plan` requires:

- plan JSON path;
- expected plan hash;
- reviewer identifier;
- ticket/change identifier;
- output directory;
- exact confirmation phrase.

## Confirmation phrase

Required exact phrase:

`I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN`

Missing or incorrect phrase fails sign-off.

## Sign-off artifact model

Generated sign-off artifacts:

- `ownership-backfill-plan-signoff-{signoffId}.json`
- `ownership-backfill-plan-signoff-{signoffId}.md`

Artifact fields include:

- `SignoffId`, `PlanId`, `PlanHash`, `PlanPath`;
- `Reviewer`, `Ticket`, `SignedAtUtc`, `ExpiresAtUtc`;
- `ConfirmationPhraseAccepted=true` (raw confirmation phrase is not stored);
- `ToolStage=P6-06`;
- `NonClaims`.

## Plan hash verification

`signoff-plan` loads plan JSON and verifies:

- plan parses successfully;
- `SummaryDraft.Mode == PlanOnly`;
- `PlanHash == --expected-plan-hash`.

Hash mismatch returns exit code `2`.

## Expiration policy

- `--expires-at` must be a future UTC timestamp when provided.
- Expired sign-off cannot be used by future apply precondition checks.

## Future apply relationship

Passed evidence gate is necessary but not sufficient for apply readiness.
Future apply preconditions require all of:

- passed gate result;
- plan artifact;
- sign-off artifact;
- passed apply-readiness artifact from the readiness gate;
- matching `PlanHash` between plan and sign-off;
- matching `ApplyInputHash` across readiness/apply inputs;
- non-expired sign-off.

Apply execution remains disabled in P6-06.
P6-07 test-only rehearsal can consume signed plans for simulation, but it still does not enable production apply writes.

## Safety guarantees

- No database connectivity required for sign-off.
- No ownership DB writes are executed.
- No payload/secret/token fields are permitted in signed plan artifacts.
- Apply command remains disabled and non-zero.

## Known limitations

- Sign-off validates artifact consistency, not full business correctness of every ownership decision.
- Plan signer identity is metadata-only; external identity integration remains future work.
- No apply execution path is enabled in this stage.

## Next steps

- P6-07: test-only apply executor rehearsal with no writes and strict sign-off/hash matching checks.
- P6-08+: explicit enablement process for apply execution remains future work.
## CLI command inventory reference
See [ownership-backfill-cli-command-inventory.md](ownership-backfill-cli-command-inventory.md) for canonical command list, exit codes, and redaction policy.
