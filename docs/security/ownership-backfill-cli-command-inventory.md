# Ownership Backfill CLI Command Inventory

## Purpose

This document defines the canonical command inventory, exit code model, and output safety constraints for the ownership backfill CLI.

## Scope

Covers command list, command intent, exit codes, redaction policy, generated artifact expectations, and disabled command posture.

## Non-claims

- No production apply enabled claim.
- No staging apply execution claim.
- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Commands

| Command | Stage | Purpose | writesToDb | requiresDbConnection | applyEnabled |
|---|---|---|---|---|---|
| `dry-run` | P6-01 | Generate ownership dry-run evidence (no-data or read-only DB scan mode). | false | false by default; true only when `--database-provider` and `--connection-string` are explicitly supplied | false |
| `validate-evidence` | P6-03 | Validate dry-run evidence structure/thresholds/ambiguity policy and emit gate result. | false | false | false |
| `plan-apply` | P6-05 | Build deterministic no-write apply plan artifacts from passed evidence gate. | false | false | false |
| `signoff-plan` | P6-06 | Validate plan hash and reviewer metadata; emit sign-off artifact. | false | false | false |
| `validate-apply-readiness` | P6-08 | Validate full evidence hash-chain + previous-values/rollback readiness. | false | false | false |
| `validate-staging-preflight` | P6-11 | Validate staging preflight inputs and environment guard contract (no execution). | false | false | false |
| `validate-staging-acceptance` | P6-12 | Validate staging post-run evidence contract and acceptance criteria. | false | false | false |
| `validate-production-promotion` | P6-13 | Validate production promotion readiness and cross-environment separation. | false | false | false |
| `apply` | P6-04 | Disabled boundary placeholder for future apply command contract. | false | false in current disabled path | false |

## Exit codes

- `0`: Success.
- `1`: Invalid input or disabled command boundary.
- `2`: Governance validation failed (rejected evidence/readiness/acceptance/promotion).

## Redaction policy

- Redact values for args: `--connection-string`, `--password`, `--token`, `--api-key`, `--apikey`, `--secret`.
- Redact connection-string-like fragments such as `Data Source=...`, `Host=...`, `Server=...`, `Username=...`, `Password=...`.
- Parser/CLI errors may include argument names, but must not print secret-like values.
- Help/examples must use placeholders only.

## Generated artifacts

- Commands may generate evidence/plan/gate/signoff/readiness/acceptance/promotion artifacts by stage.
- Generated outputs are intended for ignored artifact paths (for example `artifacts/ownership-backfill/`).
- Machine-readable command inventory: `docs/security/ownership-backfill-cli-command-inventory.json`.

## Disabled commands

- `apply` remains intentionally disabled and returns non-zero.
- Disabled apply message remains explicit that no ownership metadata was written.

## Examples

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- dry-run --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-evidence --input <path> --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- plan-apply --evidence <path> --gate-result <path> --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- signoff-plan --plan <path> --expected-plan-hash <hash> --reviewer <name-or-id> --ticket <ticket> --output <path> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-apply-readiness --dry-run <path> --gate-result <path> --plan <path> --signoff <path> --previous-values <path> --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-staging-preflight --environment Staging --apply-input-hash <hash> --readiness-result <path> --plan <path> --signoff <path> --backup-reference <ref> --rollback-readiness-reference <ref> --operator <id> --schema-version <version> --enable-staging-apply --confirm-no-production-connection
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-staging-acceptance --apply-result <path> --post-apply-dry-run <path> --post-apply-gate-result <path> --tenant-isolation-result <reference> --regression-result <reference> --rollback-evidence <reference> --apply-input-hash <hash> --plan-hash <hash> --signoff-id <id> --readiness-id <id> --staging-preflight <reference> --operator <id> --staging-change-id <id> --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-production-promotion --staging-acceptance <path> --production-dry-run <path> --production-gate-result <path> --production-plan <path> --production-signoff <path> --production-readiness <path> --production-previous-values <path> --production-change-request-id <id> --output <path>
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence <path> --gate-result <path> --plan <path> --plan-signoff <path> --output <path> --database-provider SQLite --connection-string <connection-string>
```

## Safety guarantees

- CLI UX cleanup does not enable production/staging apply.
- CLI UX cleanup does not change ownership backfill execution semantics.
- No DB writes are executed by current command set.
- No connection strings or secret-like values should be printed in normalized error output.

## Known limitations

- `apply` remains disabled by design in current stage.
- Command help is intentionally concise and focused on safe placeholders.
- Some stage-specific artifact schemas are validated by command-level gates rather than by help output.

