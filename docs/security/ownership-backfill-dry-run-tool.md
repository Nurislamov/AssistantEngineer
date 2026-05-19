# Ownership Backfill Dry-Run Tool

## Purpose

This document defines the P6-01 dry-run-only tool skeleton for ownership backfill readiness evidence.

## Scope

The P6-01 tool scope is limited to:

- ownership metadata scanning entrypoint;
- dry-run metrics collection model;
- unresolved/ambiguous record evidence export model;
- no-data dry-run execution for safe local/test usage;
- no-write safety constraints.

P6-02 extends this with a read-only database scanner while preserving dry-run-only behavior.

## Non-claims

- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Command usage

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- dry-run --output <directory> [--batch-size <int>] [--max-unresolved-rate <double>] [--connection-string <value>] [--database-provider <PostgreSQL|SQLite|None>] [--include-legacy-unscoped <true|false>]
```

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-evidence --input <evidenceDir> --output <gateOutputDir>
```

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- plan-apply --evidence <dryRunDir> --gate-result <gateResultJson> --output <planOutputDir>
```

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- signoff-plan --plan <planJson> --expected-plan-hash <hash> --reviewer <id> --ticket <changeId> --output <signoffOutputDir> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN
```

Supported command aliases:

- `--help`
- `-h`
- `help`

## Supported mode

- `dry-run` is supported.
- default mode is no-data dry-run when provider is `None` (or `--database-provider` is omitted).
- database dry-run is supported when provider is `SQLite` or `PostgreSQL` and connection string is explicitly supplied.

## Unsupported apply mode

- `apply` is explicitly rejected in P6-01.
- tool exits non-zero and reports that apply mode is designed but disabled.
- P6-03 keeps apply mode rejected and introduces a separate dry-run evidence gate (`validate-evidence`).
- P6-04 introduces apply command design/precondition validation, but apply remains disabled and non-zero with no write execution.
- P6-05 introduces `plan-apply` for deterministic plan artifact generation only; apply remains disabled and non-zero.
- P6-06 introduces `signoff-plan` governance artifact generation only; apply remains disabled and non-zero.

## Evidence outputs

Dry-run writes run-scoped evidence files under the provided output directory:

- `ownership-backfill-dry-run-summary-{runId}.json`
- `ownership-backfill-dry-run-summary-{runId}.md`
- `ownership-backfill-unresolved-records-{runId}.json`
- `ownership-backfill-previous-values-{runId}.json`

## No-data dry-run behavior

- Runs without database connectivity by default.
- Produces valid zero-count dry-run summary and metrics shape.
- Produces empty unresolved/previous-value records arrays.
- Includes non-claims in generated summary output.

## Database dry-run behavior

- Requires explicit `--database-provider` (`SQLite` or `PostgreSQL`) plus `--connection-string`.
- Uses read-only EF Core `AsNoTracking` scanning.
- Computes metrics from existing persisted records and emits unresolved/ambiguous evidence.
- Still does not execute apply/backfill and does not perform persistence writes.

## Safety guarantees

- No backfill apply/write mode in P6-01.
- No persistence-layer writes.
- No destructive SQL operations.
- No secret/payload logging.
- Connection string values are not echoed in parser/CLI error messages.
- Output files are created only under requested output directory.

## Testing strategy

- parser tests verify required arguments, defaults, and apply rejection.
- scanner tests verify deterministic no-data summary shape.
- evidence writer tests verify file generation and JSON parseability.
- CLI tests verify exit codes and secret-safe output behavior.
- governance tests verify no-write/no-apply constraints and documentation linkage.

## Known limitations

- Dry-run output currently models evidence structure, not production dataset statistics.
- Ownership backfill apply mode remains staged for later P6 steps.
- Provider `None` remains no-data mode for compatibility and safety.

## Next steps

- P6-03: validate dry-run evidence and unresolved thresholds.
- P6-04: design apply mode (still disabled) with explicit gate prerequisites.
