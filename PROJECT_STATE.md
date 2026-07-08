# AssistantEngineer Project State

<!-- GREE-ALICE-STATE:START -->
## GREE-ALICE current checkpoint

### Current stage

GREE-ALICE-23 — CLOSED / full validation PASS / awaiting push.

Latest pushed commit before GREE-ALICE-23:

```text
21f44f15 GREE-ALICE-22a Fix PROJECT_STATE checkpoint encoding
```

Current local stage prepared for validation:

```text
GREE-ALICE-23 Add CONNECT-only human safety review checklist
```

### Completed stages

```text
GREE-ALICE-00 — CLOSED / pushed — bridge placement decision
GREE-ALICE-01 — CLOSED / pushed — cloud probe plan
GREE-ALICE-02 — CLOSED / pushed — Gree Cloud probe tool scaffold
GREE-ALICE-03 — CLOSED / pushed — Gree+ Cloud login and discovery
GREE-ALICE-04 — CLOSED / pushed — normalized Gree Cloud device snapshot
GREE-ALICE-05 — CLOSED / pushed — safe raw cloud properties snapshot
GREE-ALICE-06 — CLOSED / pushed — read-only live status probe
GREE-ALICE-07 — CLOSED / pushed — live/control channel findings
GREE-ALICE-08 — CLOSED / pushed — read-only MQTT channel probe
GREE-ALICE-09 — CLOSED / pushed — offline MQTT auth/topic model draft
GREE-ALICE-10 — CLOSED / pushed — MQTT CONNECT-only safety review
GREE-ALICE-11 — CLOSED / pushed — MQTT CONNECT input contract
GREE-ALICE-12 — CLOSED / pushed — MQTT CONNECT input validation scaffold
GREE-ALICE-13 — CLOSED / pushed — control action capture evidence summary
GREE-ALICE-14 — CLOSED / pushed — MQTT auth/topic evidence acquisition plan
GREE-ALICE-15 — CLOSED / pushed — masked MQTT evidence inventory
GREE-ALICE-16 — CLOSED / pushed — MQTT evidence gate decision
GREE-ALICE-17 — CLOSED / pushed — MQTT CONNECT-only safety specification
GREE-ALICE-18 — CLOSED / pushed — CONNECT-only input contract guard tests
GREE-ALICE-19 — CLOSED / pushed — CONNECT-only dry-run command contract
GREE-ALICE-20 — CLOSED / pushed — CONNECT-only dry-run guard tests
GREE-ALICE-21 — CLOSED / pushed — CONNECT-only dry-run operator guide
GREE-ALICE-22 — CLOSED / pushed — MQTT CONNECT readiness gate
GREE-ALICE-23 — CLOSED / full validation PASS — CONNECT-only human safety review checklist
```

### Validation status

```text
Latest full validation for GREE-ALICE-22:
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
Result: PASS
Tests: 5426/5426
git diff --check: PASS
Push: PASS
master == origin/master after commit f3c0d1a0
```

### Safety boundary

```text
Workstream remains isolated in:
tools/AssistantEngineer.Tools.GreeCloudProbe
docs/integrations/gree-alice
tests/AssistantEngineer.Tests/GreeAlice

Do not connect this workstream to AssistantEngineer.Api.
Do not connect this workstream to Telegram.
Do not add runtime config.
Do not add deployment changes.
Do not add migrations.
Do not commit artifacts, PCAP, CSV, real credentials, token/password/device key/MAC/account identifiers.
Do not add live MQTT CONNECT, SUBSCRIBE, PUBLISH, or device control before an explicit separate safety stage.
Do not mention third-party repo/source names in docs, README, or PROJECT_STATE.
```

### GREE-ALICE-22 scope

```text
Added offline MQTT CONNECT readiness gate.
Reads masked dry-run report only.
Allows only ready-for-human-live-safety-review decision, not live CONNECT approval.
Keeps live CONNECT blocked pending explicit human approval.
No MQTT CONNECT implementation.
No DNS/TCP/TLS/MQTT network operation.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
```

### Files changed by GREE-ALICE-22

```text
tools/AssistantEngineer.Tools.GreeCloudProbe/Program.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/README.md
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectReadinessGateCommand.cs
docs/integrations/gree-alice/mqtt-connect-readiness-gate.md
docs/integrations/gree-alice/mqtt-connect-dry-run-operator-guide.md
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectReadinessGateSafetyTests.cs
docs/integrations/gree-alice/mqtt-connect-human-safety-review-checklist.md
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectHumanSafetyReviewChecklistTests.cs
PROJECT_STATE.md
```

### Current blocker

```text
None for GREE-ALICE-22.
```

### Next step

```text
GREE-ALICE-24 — CONNECT-only safety review decision record
```

GREE-ALICE-24 should record a CONNECT-only safety review decision only. It should still not implement live MQTT CONNECT, SUBSCRIBE, PUBLISH, device control, API integration, Telegram integration, runtime config, deployment, or migrations.
<!-- GREE-ALICE-STATE:END -->


## Current stage

ED-24BOT.CORE1 — CLOSED / production PASS.

Bot architecture audit and first behavior-preserving channel-neutral diagnostic core extraction are merged to master and deployed to production.

Production context:

```text
PR: #60
Merge commit: 52ca74a8
Audit commit: 2a056480
Core extraction commit: 118009fc
State checkpoint commit before production smoke: d4437864
VPS deployed commit: 52ca74a8
```
## Current branch

master
## Last completed work

### ED-24BOT.AUDIT1

ED-24BOT.AUDIT1 is closed.

The architecture audit documented the current Telegram bot / equipment diagnostics flow, identified channel-neutral diagnostic core boundaries, Telegram adapter boundaries, web-reuse candidates, missing tests, and production risks.

Audit report:

```text
docs/engineering/ED-24BOT-AUDIT1-bot-architecture-audit.md
```

Commit:

```text
2a056480 ED-24BOT.AUDIT1 Document bot architecture audit
```

### ED-24BOT.CORE1

ED-24BOT.CORE1 is closed and production-smoked.

Implementation commit:

```text
118009fc ED-24BOT.CORE1 Extract channel-neutral diagnostic core
```

State checkpoint commit:

```text
d4437864 ED-24BOT.CORE1 Update project state
```

Merge commit:

```text
52ca74a8 Merge pull request #60 from Nurislamov/ed-24bot-architecture-core
```

Implemented first behavior-preserving channel-neutral diagnostic core slice:

```text
- added neutral IEquipmentDiagnosticCore;
- added EquipmentDiagnosticCore and internal EquipmentDiagnosticCoreEngine;
- added DiagnosticCoreRequest / DiagnosticCoreResult and neutral diagnostic contracts;
- added neutral identity, ambiguity, audience, guidance, source and safety contracts;
- added EquipmentDiagnosticBotCompatibilityMapper;
- EquipmentDiagnosticBotService now delegates through the compatibility adapter;
- existing /api/v1/equipment-diagnostics/bot/diagnose route and bot DTO JSON shape preserved;
- no new public endpoint added;
- no Telegram runtime files changed.
```

Production deploy:

```text
VPS: assistantengineer-beta-01
Repository path: /opt/assistantengineer
Deploy path: /opt/assistantengineer/deploy
API container: assistantengineer-assistantengineer-api-1
Deployed commit: 52ca74a8
API rebuild/recreate: PASS
Telegram polling startup: PASS
```

Production Telegram smoke passed for:

```text
Gree GMV6 HR C0
Gree GMV6 HR A2
Gree GMV X oC
Gree ERV FHBQG-D10B-K E6
Gree GMV6 Uy
Gree U-Match GUD71PH1/B-S E9
/last
```

Smoke log range:

```text
UpdateId 41768632–41768638
Sending Telegram response: observed
Telegram polling update processed: observed
Status: Processed
```
## Current blocker

None.

ED-24BOT.CORE1 is production PASS.
## Important decisions

CORE1 is compatibility-first.

Existing `/api/v1/equipment-diagnostics/bot/diagnose` route and bot DTO JSON contracts must remain stable.

Telegram runtime code was intentionally not changed in CORE1.

Manual library, service requests, Telegram history extraction, parser delegation, and web API work remain out of scope for CORE1.

The internal core engine still reuses legacy bot semantic types to guarantee exact first-slice compatibility. Deeper cleanup must be a separately reviewed stage.

Do not use `docker compose --remove-orphans` on production automatically. The healthy `assistantengineer-postgres-1` orphan warning is known and treated as production data-bearing infrastructure unless separately reviewed.
## Files changed recently

ED-24BOT.AUDIT1:

```text
docs/engineering/ED-24BOT-AUDIT1-bot-architecture-audit.md
```

ED-24BOT.CORE1:

```text
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Diagnostics/**
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotService.cs
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotCompatibilityMapper.cs
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/EquipmentDiagnosticsModuleServiceCollectionExtensions.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticCoreArchitectureTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticCoreCompatibilityTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticBotServiceTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/MultiSourceDiagnosticReferenceTests.cs
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectInputContractSafetyTests.cs
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectDryRunContractSafetyTests.cs
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectReadinessGateSafetyTests.cs
docs/integrations/gree-alice/mqtt-connect-human-safety-review-checklist.md
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectHumanSafetyReviewChecklistTests.cs
PROJECT_STATE.md
```

Protected scope unchanged:

```text
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/**
data/equipment-diagnostics/error-knowledge/gree/**
data/equipment-diagnostics/manual-library/manuals.json
src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations/**
deploy/**
scripts/deployment/**
scripts/operations/**
runtime config/env files
```
## Validation status

ED-24BOT.AUDIT1 validation:

```text
Build: PASS, 0 warnings/errors
Tests: PASS, 5391/5391
git diff --check: PASS
Scope checks: PASS
Runtime, Telegram, diagnostics JSON, manuals, migrations and deployment unchanged
```

ED-24BOT.CORE1 local/CI validation:

```text
Restore: PASS
Build: PASS, 0 warnings/errors
Core/bot focused tests: 79/79 PASS
Telegram-focused tests: 565/565 PASS
Full test suite: 5411/5411 PASS
git diff --check: PASS
Scope checks: PASS
Architecture guard: PASS
PR #60 checks: PASS, 10/10 successful
```

ED-24BOT.CORE1 production validation:

```text
VPS deployed commit: 52ca74a8
API rebuild/recreate: PASS
Container status: Up
Telegram command menu synchronized: PASS
Telegram polling started: PASS
Telegram deleteWebhook on startup succeeded: PASS
Telegram smoke logs: PASS
Updates processed: 41768632–41768638
```

Expected known production warnings:

```text
docker compose orphan warning for assistantengineer-postgres-1: known, do not auto-remove
Microsoft.AspNetCore.Hosting.Diagnostics[15] HTTP_PORTS/URLS warning: known, non-blocking
```

No observed production smoke errors:

```text
InvalidOperationException: not observed
Telegram polling batch failed: not observed
error/exception: not observed
duplicate skipped: not observed
```

Visual Telegram smoke: PASS.
## Known backlog

CI maintenance:

- Node.js 20 deprecation warning remains future maintenance.

Flaky/infrastructure watch:

- SQLite idempotency integration test has historical flakiness:
  - EngineeringWorkflowSqliteProviderPersistsIdempotencyAcrossFactoryRestart

## Next step

Recommended next stage:

```text
ED-24BOT.CORE2 or ED-24BOT.CORE1.POST — small follow-up only after separate review
```

Possible follow-ups, do not start automatically:

```text
- reduce internal dependency of EquipmentDiagnosticCoreEngine on legacy bot semantic types;
- add neutral diagnostic text interpreter only after separate parser-delegation stage;
- design neutral history event projection only after separate history stage;
- keep MANUAL1, REQUEST1 and WEB1 out of scope until separately planned.
```

For a new chat, start from this checkpoint:

```text
ED-24BOT.AUDIT1 — CLOSED
ED-24BOT.CORE1 — CLOSED / production PASS
master top after PR #60: 52ca74a8
production VPS deployed commit: 52ca74a8
Telegram smoke: PASS
```
