# AssistantEngineer Project State

## Current stage

ED-24UMATCH.3 CLOSED / production PASS.

Gree U-Match R32 diagnostics audit, CI validation, production deployment and Telegram smoke are completed.

## Current branch

master

Latest known master commit:

- 4159818d Merge pull request #54 from Nurislamov/ed-24umatch-1-2-diagnostics-audit

## Last completed work

### ED-24GMVMINI

ED-24GMVMINI is closed.

ED-24GMVMINI.1-2 audited GMV Mini diagnostics and runtime baseline.

- PR: #52
- Merge commit: b781d872
- Implementation commit: 27ec74a0
- GMV Mini cards: 148 total
- Indoor: 27
- Outdoor: 62
- Status: 59
- Production smoke: PASS

ED-24GMVMINI.4 fixed visible wording for GMV Mini q/n function-setting cards.

- PR: #53
- Merge commit: 4de47413
- Implementation commit: f6e04698
- State update commit: f8c5bbb6
- Production smoke: PASS
- Production logs: clean

### ED-24UMATCH

ED-24UMATCH.1-2 audited Gree U-Match R32 diagnostics against the U-Match DC Inverter Unit service manual.

- PR: #54
- Merge commit: 4159818d
- Implementation commit: abdd72a0
- Manual identity:
  - U-MATCH DC INVERTER UNIT
  - GC202209-I
  - R32
  - 3.5kW~16.0kW
  - Operation range: -15℃~48℃
- U-Match runtime count: 107 cards
- Gree runtime count: 1308
- All 107 U-Match R32 cards were normalized under:
  - data/equipment-diagnostics/error-knowledge/gree/umatch-r32/system/**
- Package metadata updated:
  - data/equipment-diagnostics/error-knowledge/packages/gree-umatch-r32-error-codes.json
- Manual registry updated:
  - data/equipment-diagnostics/manual-library/manuals.json
- Focused tests added/updated:
  - tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeUMatchManualAudit1_2Tests.cs
  - tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeUMatchErvImport24Tests.cs
  - tests/AssistantEngineer.Tests/EquipmentDiagnostics/ManualCoverageRegistryTests.cs

ED-24UMATCH.3 deployed U-Match R32 diagnostics to production and passed Telegram smoke.

Smoke checked:

- Gree U-Match E0
- Gree U-Match E1
- Gree U-Match E2
- Gree U-Match E3
- Gree U-Match E4
- Gree U-Match E6
- Gree U-Match E9
- Gree U-Match C0
- Gree U-Match C6
- Gree U-Match F3
- Gree U-Match H5
- Gree U-Match HC
- Gree U-Match Lc
- Gree U-Match U7
- Gree U-Match qC
- Gree U-Match PA
- Gree U-Match PL
- Gree U-Match PH
- Gree U-Match C8
- Gree U-Match EL
- Gree U-Match Fo
- Gree U-Match H1
- Gree U-Match CL
- Gree U-Match d1
- Gree U-Match GUD125W1/NhB-S E1
- Gree U-Match GUD160PHS1/B-S E0
- Gree U-Match GUD71PH1/B-S E9
- /last

Production logs were clean. Telegram updates 41768405 through 41768417 were processed successfully.

## Current blocker

None.

## Important decisions

U-Match R32 is a semi-commercial split product family, not GMV/VRF.

Do not mix U-Match runtime cards with:

- GMV6
- GMV X
- GMV Mini
- GMV9 Flex

U-Match status/mode codes must remain non-alarming:

- Fo — refrigerant recovery/service mode, not a component fault
- H1 — ordinary defrosting, not a component fault
- CL — automatic cleaning, not a component fault
- d1/d2/d3 — DRED demand response modes, not component faults

U-Match communication codes must stay distinct:

- E6 — outdoor unit and indoor unit communication error
- C0 — wired controller and indoor unit communication failure

U-Match E1/E3 pressure protection wording may mention that pressure switch protection applies to 125/140/160 outdoor units where relevant.

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

ED-24UMATCH.1-2:

- data/equipment-diagnostics/error-knowledge/gree/umatch-r32/system/**
- data/equipment-diagnostics/error-knowledge/packages/gree-umatch-r32-error-codes.json
- data/equipment-diagnostics/manual-library/manuals.json
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeUMatchManualAudit1_2Tests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeUMatchErvImport24Tests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/ManualCoverageRegistryTests.cs

No production env changes.
No EF migrations.
No generated artifacts committed.

## Validation status

ED-24UMATCH.1-2:

- Focused U-Match tests: 66/66 PASS
- Full suite: 5353/5353 PASS
- Engineering Core V1 verifier: PASS
- PR checks: 7/7 PASS
- Master CI after merge: PASS
- git diff --check: PASS
- Non-U-Match Gree runtime JSON changes: none
- U-Match runtime count: 107
- Gree runtime count: 1308

ED-24UMATCH.3:

- VPS deploy: PASS
- Telegram smoke: PASS
- Production logs: clean

## Known backlog

ED-24UX.LAST — Improve /last display to preserve matched series/model label.

Current behavior:

- /last works and preserves the latest code and meaning.
- It may display a short label such as `Gree E9` instead of preserving the matched series/model label such as `Gree U-Match R32 E9` or `Gree GMV Mini C0`.

This is not a production blocker.

ED-24OPS.CLEANUP — Move VPS env backups outside repo or ignore safe backup pattern.

CI maintenance:

- Node.js 20 deprecation warning remains future maintenance.

Flaky/infrastructure watch:

- SQLite idempotency integration test has historical flakiness:
  - EngineeringWorkflowSqliteProviderPersistsIdempotencyAcrossFactoryRestart

## Next step

Recommended next stage:

ED-24UX.LAST — improve /last display to preserve matched series/model label.

Alternative:

ED-24OPS.CLEANUP — cleanup VPS env backup handling.
