# Ownership Backfill Database Dry-Run Scanner

## Purpose

This document defines the P6-02 read-only database scanner for ownership backfill dry-run evidence.

## Scope

The scanner performs read-only ownership coverage analysis for:

- `Project`
- `Building`
- `WorkflowState`
- `Scenario`
- `Job`
- `JobEvent`
- `ScenarioHistory`

It computes resolvable/unresolved/ambiguous ownership metrics and exports unresolved evidence through the existing dry-run evidence writer.

## Non-claims

- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Supported providers

- `SQLite`
- `PostgreSQL`
- `None` (routes to no-data scanner, not database scanner)

## Command usage

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- dry-run --output <directory> --database-provider SQLite --connection-string "<connection>"
```

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- dry-run --output <directory> --database-provider PostgreSQL --connection-string "<connection>"
```

## Read-only guarantees

- Scanner uses EF Core `AsNoTracking()` read queries.
- Scanner never calls `SaveChanges` or `SaveChangesAsync`.
- Scanner does not execute `UPDATE`/`INSERT`/`DELETE`/`TRUNCATE` SQL.
- Scanner does not run apply mode.

## Ownership resolution rules

Implemented hierarchy follows `docs/security/ownership-backfill-strategy.md`:

- `Project`: `OrganizationId`
- `Building`: parent project organization
- `WorkflowState`: project, then building->project
- `Scenario`: project, then building->project
- `Job`: scenario, then project
- `JobEvent`: job, then scenario, then project
- `ScenarioHistory`: scenario, then project

When multiple sources resolve to different `OrganizationId` values, record is marked ambiguous and unresolved.

## Metrics

Scanner outputs:

- per-record-type `TotalRecords`, `ResolvableRecords`, `UnresolvedRecords`, `AmbiguousRecords`, `ResolvableRate`, `UnresolvedByReason`
- summary `TotalRecordsScanned`, `TotalRecordsResolvable`, `TotalRecordsUnresolved`, `UnresolvedByReason`

## Unresolved records

Unresolved records export includes:

- `recordType`
- `recordId`
- `reason`
- `candidateProjectId`
- `candidateBuildingId`
- `candidateOrganizationId`
- `notes`

No payload fields are exported.

## Ambiguous records

Ambiguous ownership is treated as unresolved:

- `reason` uses record-type-specific ambiguous reason (for example `ScenarioOwnershipAmbiguous`);
- `AmbiguousRecords` is incremented in per-type metrics;
- unresolved evidence row is emitted.

## Safety checks

- Database scanner runs only when provider is `SQLite` or `PostgreSQL` and connection string is explicitly supplied.
- Missing connection string for database provider fails fast.
- Provider `None` routes to no-data scanner.
- Connection strings are never printed.

## Known limitations

- Scanner does not modify data and does not perform ownership backfill apply.
- Workflow persistence metadata lacks direct `WorkflowId`/`BuildingId` on some record types; coverage remains partial where source metadata is partial.
- Scanner relies on existing persisted metadata and does not infer hidden ownership.

## Next steps

- P6-03: evidence validation gates and unresolved-threshold policy.
- P6-04: apply-mode design (still disabled) with explicit gate prerequisites.
- P6-05: deterministic `plan-apply` generation from passed evidence while apply remains disabled.
## CLI command inventory reference
See [ownership-backfill-cli-command-inventory.md](ownership-backfill-cli-command-inventory.md) for canonical command list, exit codes, and redaction policy.
