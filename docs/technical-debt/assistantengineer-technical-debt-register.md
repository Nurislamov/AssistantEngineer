# AssistantEngineer Technical Debt Register

## Last updated

ED-24EF.2

## Current stable point

- Production baseline: `8ab72517` with ED-24EF.1 production PASS.
- ED-24EF.2 is locally validated only until a VPS deploy/live-check is completed.
- Runtime counts: Gree 1184; GMV6 HR 262; GMV6 263; GMV Mini 136; GMV X 263; GMV9 Flex 260.
- Manual policy: ServiceManual and InstallationManual are library-only; diagnostic guide delivery is OwnerManual-only.

## P0 / blocking

No current blocker was found.

## P1 / should fix soon

No active P1 item was found.

## P2 / planned cleanup

### TD-BUILD-001 - Nullable warnings in architecture guard tests

- Area: build/tests
- Severity: P2
- Current evidence: test compilation reports six CS8602 warnings in `AuditLogSecurityGuardTests`,
  `BuildingInputValidationP3RefactorGuardTests`, and `P3BuildingInputValidationRefactorGuardTests`.
- Risk: warning noise can hide a new nullable regression.
- Suggested next stage: ED-24QA.2
- Notes: production projects build; warnings are currently confined to test guard code.

### TD-TG-001 - Telegram identity duplicates remain representable

- Area: Telegram/persistence
- Severity: P2
- Current evidence: `TelegramChatId` is unique, while `TelegramUserId` has a non-unique index; deterministic manager-first
  duplicate handling is covered by existing tests and project-state notes.
- Risk: stale duplicate rows increase authorization reasoning and maintenance complexity even though current selection is deterministic.
- Suggested next stage: ED-24USR.4
- Notes: audit production data and define merge/uniqueness rules before changing constraints.

### TD-MAN-001 - Manual coverage and exact-family selection remain incomplete

- Area: manual library
- Severity: P2
- Current evidence: GMV9 Flex OwnerManual remains unavailable/pending; InstallationManual coverage is incomplete; GMV Mini /
  Slim uses a multi-file bucket rather than exact model-family matching.
- Risk: an eligible user may see no guide or must choose among broad family documents.
- Suggested next stage: ED-24MAN.3
- Notes: keep ServiceManual and InstallationManual library-only and diagnostics OwnerManual-only.

### TD-DOC-001 - Manual coverage documents lag production binding state

- Area: docs/project-state
- Severity: P2
- Current evidence: `gree-gmv6-hr-manual-coverage.md` and `telegram-manual-library-plan.md` still describe GMV6 HR
  OwnerManual delivery as unavailable, while the latest production live-check confirms delivery.
- Risk: operators may follow stale availability guidance.
- Suggested next stage: ED-24DOC.1
- Notes: not corrected in ED-24EF.1 because this stage changes only sentinel behavior and its required state/debt records.

### TD-OPS-001 - DataProtection keys are stored inside the application container

- Area: operations/configuration
- Severity: P2
- Current evidence: production logs warn that keys under `/home/app/.aspnet/DataProtection-Keys` may not persist outside
  the container.
- Risk: container replacement can invalidate protected payloads that depend on the ephemeral key ring.
- Suggested next stage: ED-24OPS.3
- Notes: intentionally not changed in ED-24EF.2.

## P3 / backlog

### TD-TOOL-001 - EF CLI patch version trails runtime

- Area: CI/dependencies
- Severity: P3
- Current evidence: local `dotnet ef` is 10.0.5 while the EF runtime packages are 10.0.6.
- Risk: minor tooling diagnostics or scaffolding behavior may differ from the runtime patch.
- Suggested next stage: ED-24TOOL.1
- Notes: `migrations has-pending-model-changes` still completed successfully for ED-24EF.1 and ED-24EF.2.

## Resolved in ED-24EF.2

### TD-EF-002 - HourlySchedule Factors had no value comparer

- Area: EF/persistence
- Severity: resolved
- Current evidence: the JSON-converted `HourlySchedule.Factors` sequence previously used reference-based tracking and
  produced an EF model warning.
- Resolution: a typed comparer now uses ordered sequence equality, element-based hashing, and a cloned array snapshot.
- Validation: model metadata, SQLite round-trip, equivalent replacement, in-place element mutation, and persisted update
  are covered by focused tests.
- Migration: no migration required; EF reports no pending model changes because database schema is unchanged.

## Resolved in ED-24EF.1

### TD-EF-001 - Telegram enum database defaults had no explicit sentinel

- Area: EF/persistence
- Severity: resolved
- Current evidence: `RequestedRole`, `DocumentType`, and `MinRole` had database-generated defaults without a configured
  sentinel, producing EF model warnings and risking substitution of valid CLR-zero enum values.
- Resolution: each property now uses an out-of-domain `-1` sentinel while retaining its existing database default.
- Validation: model metadata and SQLite round-trip tests cover `Owner`, all required manual document types, and multiple
  minimum roles.
- Migration: no migration required; EF reports no pending model changes because database schema/default constraints are unchanged.
