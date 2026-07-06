# AssistantEngineer Project State

## Current stage

ED-24BOT.CORE1 — CLOSED locally / validation PASS / awaiting Telegram smoke before merge
## Current branch

ed-24bot-architecture-core
## Last completed work

ED-24BOT.AUDIT1 completed as documentation-only architecture audit.

ED-24BOT.CORE1 completed with commit:

```text
118009fc ED-24BOT.CORE1 Extract channel-neutral diagnostic core
```

Implemented first behavior-preserving channel-neutral diagnostic core slice:

```text
- added neutral IEquipmentDiagnosticCore;
- added EquipmentDiagnosticCore and internal core engine;
- added DiagnosticCoreRequest / DiagnosticCoreResult and neutral diagnostic contracts;
- added EquipmentDiagnosticBotCompatibilityMapper;
- EquipmentDiagnosticBotService now delegates through the compatibility adapter;
- existing public bot DTO/API shape preserved;
- no new public endpoint added.
```
## Current blocker

No code blocker.

Manual Telegram smoke is required before merge.
## Important decisions

CORE1 is compatibility-first.

Existing `/api/v1/equipment-diagnostics/bot/diagnose` route and bot DTO JSON contracts must remain stable.

Telegram runtime code was intentionally not changed in CORE1.

Manual library, service requests, Telegram history extraction, parser delegation, and web API work remain out of scope for CORE1.

The internal core engine still reuses legacy bot semantic types to guarantee exact first-slice compatibility. Deeper cleanup must be a separately reviewed stage.
## Files changed recently

```text
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Diagnostics/**
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotService.cs
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotCompatibilityMapper.cs
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/EquipmentDiagnosticsModuleServiceCollectionExtensions.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticCoreArchitectureTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticCoreCompatibilityTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticBotServiceTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/MultiSourceDiagnosticReferenceTests.cs
```
## Validation status

ED-24BOT.CORE1 validation:

```text
Restore: PASS
Build: PASS, 0 warnings/errors
Core/bot focused tests: 79/79 PASS
Telegram-focused tests: 565/565 PASS
Full test suite: 5411/5411 PASS
git diff --check: PASS
Scope checks: PASS
Architecture guard: PASS
Telegram runtime files: unchanged
diagnostics JSON/manuals/migrations/deployment/config/env: unchanged
```

Manual Telegram smoke: not run yet. Required before merge to master.
## Known backlog

CI maintenance:

- Node.js 20 deprecation warning remains future maintenance.

Flaky/infrastructure watch:

- SQLite idempotency integration test has historical flakiness:
  - EngineeringWorkflowSqliteProviderPersistsIdempotencyAcrossFactoryRestart

## Next step

Run manual Telegram smoke on branch build before merging to master:

```text
Gree GMV6 HR C0
Gree GMV6 HR A2
Gree GMV X oC
Gree ERV FHBQG-D10B-K E6
Gree GMV6 Uy
Gree U-Match GUD71PH1/B-S E9
/last
```

Check:

```text
matched series/model label is preserved;
/last works;
manual diagnostic flow works;
library access policy is not regressed;
old production-visible phrases do not return:
- "подтвердите код"
- "Сверьте модель"
- "карточка применима"
```
