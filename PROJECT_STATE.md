# AssistantEngineer Project State

## Current stage

ED-24OPS.CLEANUP CLOSED / local validation PASS.

Safe operations hygiene for the production Compose orphan-Postgres warning and VPS environment backup files.

Stage boundaries:

- The healthy `assistantengineer-postgres-1` container is treated as production data-bearing infrastructure.
- No container, database, volume, or environment backup deletion/removal is included.
- Production scripts do not use `docker compose --remove-orphans`.
- Existing `deploy/.env.before-*` files are ignored and may be moved outside the working tree only after operator
  confirmation that they are backup copies rather than the active `.env`.

Previous production context:

PR #56 was merged to master, deployed to the VPS, and passed production Telegram smoke for the newly quality-closed Gree diagnostic scopes.

- PR: #56
- Merge commit: 35741920
- Implementation commit: 7ddca4fa
- VPS deployed commit: 35741920
- VPS logs: clean; only Telegram polling, Sending Telegram response, and Status: Processed messages were observed.

## Current branch

ed-24ops-cleanup-orphans-env-backups

Target branch: master

Latest known deployed master commit:

- 35741920

## Last completed work

### ED-24OPS.CLEANUP

ED-24OPS.CLEANUP is closed locally.

Repository investigation found:

- The tracked `deploy/docker-compose.yml` has used the fixed project name `assistantengineer` since its initial
  commit and has never contained a PostgreSQL service.
- The current Compose file therefore reports `assistantengineer-postgres-1` as an orphan because that container
  belongs to an earlier or VPS-local Compose definition for the same project whose `postgres` service is not in
  the tracked file.
- No tracked deployment or operations script creates `deploy/.env.before-*`; those files came from an
  external/manual VPS procedure.

Chosen handling:

- `deploy/.gitignore` ignores only `.env.before-*` backup copies in addition to the already ignored active `.env`.
- `docs/operations/production-compose-hygiene.md` documents non-destructive Postgres inspection and prohibits
  automatic orphan removal.
- Future environment backups should use `/opt/assistantengineer-runtime-backups/env`; moving existing backups is
  a manual operator step after confirming they are not the active `.env`.
- No production deployment script was changed.

### ED-24LIB.ACCESS1

ED-24LIB.ACCESS1 is closed locally.

The Telegram manual-library access policy is now documented in `docs/equipment-diagnostics/telegram-manual-library-plan.md` and protected by tests.

Implementation outcome:

- General manual-library browsing and file delivery are controlled by explicit library access, active/enabled user state, active binding state, library visibility, and visible document type.
- Once explicit library access is granted, the library does not apply an additional Installer/Engineer subdivision for active visible files.
- `MinRole` remains stored as legacy binding metadata/upload default, but is not a second authorization split inside the general library after explicit library access is granted.
- Diagnostic guide delivery remains separate and OwnerManual-only; users without general library access can still receive eligible linked diagnostic manuals through the diagnostic context.

### ED-24UX.LAST

ED-24UX.LAST is closed.

/last now preserves the matched diagnostic family/model label from the original Telegram lookup result.

Production smoke confirmed /last after the relevant diagnostic checks, including:

- ERV B Series
- GMV X
- GMV6 HR
- GMV6 delta

### ED-24QUAL / ED-24GMV6 / ED-24REG

PR #56 closed the quality and registry stages:

- ED-24QUAL.ERV1 — cleaned ERV B Series visible diagnostic wording.
- ED-24QUAL.X1 — cleaned the remaining GMV X visible diagnostic wording.
- ED-24GMV6.DELTA — confirmed the eight GC202203-IV GMV6 delta cards.
- ED-24QUAL.HR1 — cleaned all GMV6 HR visible diagnostic wording.
- ED-24QUAL.HR2 — prepared and then production-smoked the GMV6 HR Telegram flow.
- ED-24REG.1 — reconciled manual registry statuses before production smoke.
- ED-24QUAL.PROD1 — promoted conservative manual registry production statuses after successful production smoke.

Runtime diagnostic counts remain:

- Gree total: 1308
- ERV B Series: 5
- GMV X: 263
- GMV6: 263
- GMV6 HR: 262
- GMV Mini: 148
- U-Match R32: 107
- GMV9 Flex: 260

Production smoke passed:

- ERV B Series: dF, dH, E6, L0, L9, FHBQG-D10B-K E6, /last
- GMV X: C1, C7, CU, U5, o1, oC, /last
- GMV6 HR: C0, C2, C3, CH, CL, U4, U6, U8, U9, n0, n7, A2, /last
- GMV6 delta: A9, n1, qA, qC, qH, qP, qU, Uy, /last

Manual registry production statuses promoted to `DeployedAndSmokeVerified` for the smoke-validated imported scopes:

- `gree-erv-b-series-service-manual`
- `gree-erv-wired-controller-owner-manual`
- `gree-gmv-x-service-manual-2022-09`
- `gree-gmv6-hr-service-manual-2025-07`
- `gree-gmv6-service-manual-2022-03`

The ERV installation/startup/maintenance manual remains analyzed but not runtime-imported.

## Current blocker

None.

## Important decisions

If a user is granted Telegram library access, that user is considered a trusted library user.

The Telegram manual library is not split internally by Installer/Engineer role.

A user without Telegram library access can still receive manuals through the diagnostic flow when a manual is linked to a diagnostic code.

Gree product-family boundaries remain strict. Do not mix:

- GMV6
- GMV6 HR
- GMV X
- GMV Mini
- GMV9 Flex
- U-Match R32
- ERV B Series

Status, query, commissioning, mode, and function-setting codes must not be presented as independent component failures.

Visible Telegram answers must not leak internal/provenance terms such as:

- manual
- source
- packageId
- карточка неисправности
- по таблице
- основание
- руководство
- Подтвердите код
- Сверьте модель

Do not use public claims about exact external parity.

## Files changed recently

ED-24QUAL.PROD1:

- PROJECT_STATE.md
- data/equipment-diagnostics/manual-library/manuals.json
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeDiagnosticManualRegistryReconciliationTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmv6FreshManualDelta13ATests.cs

ED-24LIB.ACCESS1:

- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Manuals/TelegramManualLibraryService.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramManualLibraryTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/TelegramUserRolePolicyTests.cs
- docs/equipment-diagnostics/telegram-manual-library-plan.md
- PROJECT_STATE.md

Scope guard:

- No diagnostic card JSON changes for ED-24LIB.ACCESS1.
- No EF migrations.
- No manual registry status changes.
- No package metadata changes.
- No GMV Mini, U-Match R32, GMV9 Flex, GMV6, GMV6 HR, GMV X, or ERV card folder changes.

## Validation status

ED-24QUAL.PROD1 local validation:

- Manual registry focused tests: 17/17 PASS
- `dotnet build .\AssistantEngineer.sln`: PASS, 0 warnings, 0 errors
- `dotnet test .\AssistantEngineer.sln`: PASS, 5375/5375
- `git diff --check`: PASS

Production validation:

- VPS deploy: PASS at 35741920
- Telegram smoke: PASS for ERV B Series, GMV X, GMV6 HR, GMV6 delta, and /last
- VPS logs: clean

ED-24LIB.ACCESS1 local validation:

- Telegram/manual-library/access focused tests: 135/135 PASS
- `dotnet build .\AssistantEngineer.sln`: PASS, 0 warnings, 0 errors
- `dotnet test .\AssistantEngineer.sln`: PASS, 5389/5389
- `git diff --check`: PASS

ED-24OPS.CLEANUP local validation:

- Deployment/operations/scripts/GitIgnore/Docker focused tests: 143/143 PASS
- `dotnet build .\AssistantEngineer.sln`: PASS, 0 warnings, 0 errors
- `dotnet test .\AssistantEngineer.sln`: PASS, 5391/5391
- `git diff --check`: PASS

## Known backlog

CI maintenance:

- Node.js 20 deprecation warning remains future maintenance.

Flaky/infrastructure watch:

- SQLite idempotency integration test has historical flakiness:
  - EngineeringWorkflowSqliteProviderPersistsIdempotencyAcrossFactoryRestart

## Next step

Recommended next stage:

Operator review and merge of ED-24OPS.CLEANUP; no automatic production cleanup is required.
