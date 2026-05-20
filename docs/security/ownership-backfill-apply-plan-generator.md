# Ownership Backfill Apply Plan Generator

## Purpose

This document defines the P6-05 `plan-apply` command that generates deterministic apply-plan evidence from passed dry-run evidence and a passed gate result without writing any ownership data.

## Scope

The P6-05 scope is limited to:

- reading dry-run evidence files;
- reading a passed gate result JSON;
- generating deterministic planned-record outputs;
- generating an apply-summary draft;
- calculating deterministic plan hash values;
- writing plan evidence artifacts.

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
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- plan-apply \
  --evidence <dry-run-dir> \
  --gate-result <gate-result-json> \
  --output <plan-output-dir>
```

Optional arguments:

- `--ruleset-version <value>` (default `P6-05`)
- `--max-planned-records <int>`
- `--include-legacy-unscoped <true|false>`
- `--force-overwrite <true|false>`

## Required inputs

`plan-apply` requires:

- dry-run summary evidence in `--evidence` directory;
- gate result JSON at `--gate-result` with `Passed=true`;
- unresolved records evidence file when summary unresolved count is non-zero;
- previous-values evidence file.

If gate result is failed, `plan-apply` exits with code `2` and no plan artifacts are emitted.

## Generated outputs

`plan-apply` writes plan-scoped artifacts:

- `ownership-backfill-apply-plan-{planId}.json`
- `ownership-backfill-apply-summary-draft-{planId}.json`
- `ownership-backfill-apply-summary-draft-{planId}.md`
- `ownership-backfill-planned-records-{planId}.json`

## Plan hash model

Plan hash uses deterministic SHA256 over canonical normalized content:

- dry-run summary normalized fields (without timestamps);
- gate result normalized fields;
- sorted planned records;
- ruleset version;
- stage token (`P6-05`).

The same evidence and ruleset produce the same `PlanHash`.

## Planned record rules

Records are planned only when all conditions are met:

- unresolved reason is in resolvable reason set;
- unresolved reason is not ambiguous;
- candidate `OrganizationId` exists;
- candidate `ProjectId` exists;
- proposed values do not conflict with current non-null values;
- update is not a destructive nulling action.

## Skipped record rules

Records are skipped when:

- reason is ambiguous;
- candidate ownership identifiers are incomplete;
- reason is non-resolvable for current ruleset;
- current non-null values conflict with proposed values;
- current values already match proposed values.

## Safety guarantees

- `plan-apply` performs no database access.
- `plan-apply` performs no ownership writes.
- apply command remains disabled in P6-05.
- no payloads, secrets, or tokens are included in planned-record artifacts.

## Relationship to apply mode

`plan-apply` does not enable apply. Future apply mode must require evidence + gate + approved `PlanHash` matching and a valid `signoff-plan` artifact before any write path is considered.
P6-07 test-only rehearsal may execute simulated updates from plan artifacts in controlled test stores only, and still does not enable production apply writes.

## Known limitations

- Plan generation relies only on evidence quality; it does not rescan database state.
- If candidate ownership is missing in evidence, plan can produce zero planned records.
- Include-legacy behavior is kept as explicit option surface and remains conservative in P6-05.

## Next steps

- P6-06: add explicit `signoff-plan` review gate before any apply enablement stage.
- P6-07: add test-only apply executor rehearsal while keeping production apply disabled.
## CLI command inventory reference
See [ownership-backfill-cli-command-inventory.md](ownership-backfill-cli-command-inventory.md) for canonical command list, exit codes, and redaction policy.
