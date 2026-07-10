# AssistantEngineer Project State

<!-- GREE-ALICE-STATE:START -->
## GREE-ALICE current checkpoint

### Current stage

GREE-ALICE-52 — APPLIED locally / validation pending.

Latest closed GREE-ALICE commit:

```text
4b0b8852 GREE-ALICE-51 Add local bridge runbook and smoke script boundary
```

Current local stage prepared for validation:

```text
GREE-ALICE-52 Add local bridge HTTP smoke endpoint boundary
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
GREE-ALICE-23 — CLOSED / pushed — CONNECT-only human safety review checklist
GREE-ALICE-24 — CLOSED / pushed — CONNECT-only safety review decision record
GREE-ALICE-25 — CLOSED / pushed — CONNECT-only operator sign-off template
GREE-ALICE-26 — CLOSED / pushed — offline review packet and fail-closed live gate policy
GREE-ALICE-30 — CLOSED / pushed — choose offline bridge skeleton path
GREE-ALICE-31 — CLOSED / pushed — offline bridge project skeleton
GREE-ALICE-32/33 — CLOSED / pushed — Yandex Smart Home offline DTO mapping and API skeleton
GREE-ALICE-34 — CLOSED / pushed — offline API contract tests and error behavior
GREE-ALICE-35 — CLOSED / committed locally — offline account and device registry boundary
GREE-ALICE-36 — CLOSED / pushed — Gree Cloud adapter interface boundary
GREE-ALICE-37 — CLOSED / pushed — read-only cloud state mapping contract
GREE-ALICE-38 — CLOSED / pushed — bridge safety middleware and kill switches
GREE-ALICE-39 — CLOSED / committed locally — offline end-to-end bridge flow tests
GREE-ALICE-40 — CLOSED / pushed — isolated staging deploy skeleton
GREE-ALICE-41 — CLOSED / pushed — live read-only adapter proposal
GREE-ALICE-42 — CLOSED / pushed — live read-only pilot gate
GREE-ALICE-43 — CLOSED / pushed — control safety approval package
GREE-ALICE-44 — CLOSED / pushed — single-device control pilot skeleton
GREE-ALICE-45 — CLOSED / pushed — minimal production pilot boundary
GREE-ALICE-46 — CLOSED / pushed — VRF/GMV child unit support
GREE-ALICE-47 — CLOSED / pushed — device registry import/admin boundary
GREE-ALICE-48 — CLOSED / pushed — Yandex account linking boundary
GREE-ALICE-49 — CLOSED / pushed — Yandex provider readiness package
GREE-ALICE-50 — CLOSED / pushed — local Yandex provider smoke harness
GREE-ALICE-51 — CLOSED / pushed — local bridge runbook and smoke script boundary
```

### Validation status

```text
Latest validation for GREE-ALICE-51:
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
Result: PASS
Tests: 5679/5679
git diff --check: PASS
Push: PASS
master == origin/master after commit 4b0b8852
```

### Safety boundary

```text
Workstream remains isolated in:
src/Integrations/GreeAliceBridge
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

### GREE-ALICE-52 local scope

```text
Adds localhost-only HTTP smoke boundary contracts.
Adds offline local HTTP smoke plan provider.
Adds local HTTP smoke endpoint checklist and request templates.
Extends PowerShell local smoke script with optional localhost-only HTTP smoke mode.
Adds tests for HTTP smoke defaults, endpoint plan, request templates, docs, script safety, and existing behavior compatibility.
Live read-only adapter remains not implemented.
Live read-only adapter remains disabled.
Live read-only pilot remains not approved.
Control approval remains not approved.
Control adapter remains dry-run fail-closed.
Single-device control pilot remains not approved.
Command sending remains disabled.
Minimal production pilot remains not approved.
Production deployment wiring remains disabled.
Read-only-first remains required before any future production pilot.
VRF/GMV gateway remains internal by default.
VRF/GMV child-unit behavior remains offline fixture / fail-closed.
Registry import/admin boundary remains offline-template only.
Real imports remain disabled.
Admin UI remains not implemented.
Gree Cloud discovery auto-exposure remains disabled.
Manual review remains required before Yandex exposure.
Stable Yandex device IDs remain required for exposed devices.
Room binding remains required for exposed devices.
Yandex account linking boundary remains offline-template only.
Real OAuth remains not implemented.
Real Yandex credentials and tokens remain forbidden in repository.
Yandex user must map to bridge account and explicit registry scope.
Unknown/unlinked users fail closed.
Yandex provider readiness remains not ready.
Provider registration remains not approved.
Provider publication remains not approved.
Production endpoint remains not configured.
Production deploy remains disabled.
Manual smoke remains required.
Security review remains required.
Local Yandex provider smoke harness remains offline-local only.
Local bridge runbook remains offline-local only.
Local bridge smoke script remains offline-local only.
Local HTTP smoke boundary remains localhost-only.
Smoke harness does not call real Yandex.
Smoke script does not call real Yandex.
HTTP smoke boundary does not call real Yandex.
Smoke harness does not implement OAuth.
Smoke script does not implement OAuth.
HTTP smoke boundary does not implement OAuth.
Smoke harness does not use real credentials or tokens.
Smoke script does not use real credentials or tokens.
HTTP smoke boundary does not use real credentials or tokens.
Smoke harness does not call live Gree+ Cloud.
Smoke script does not call live Gree+ Cloud.
HTTP smoke boundary does not call live Gree+ Cloud.
Smoke harness does not use MQTT.
Smoke script does not use MQTT.
HTTP smoke boundary does not use MQTT.
Smoke harness does not control devices.
Smoke script does not control devices.
HTTP smoke boundary does not control devices.
Smoke harness does not deploy anything.
Smoke script does not deploy anything.
HTTP smoke boundary does not deploy production.
Keeps live CONNECT blocked.
Keeps SUBSCRIBE blocked.
Keeps PUBLISH blocked.
Keeps device control blocked.
Keeps Gree+ runtime control blocked.
Keeps /action dry-run fail-closed.
No MQTT CONNECT implementation.
No HttpClient live calls.
No bridge runtime environment configuration.
No DNS/TCP/TLS/MQTT network operation.
No API/Telegram/runtime/deployment/migration changes.
No production deployment wiring.
```

### Files changed by GREE-ALICE-52

```text
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Contracts/YandexSmartHome/HttpSmoke/*
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Application/YandexSmartHome/HttpSmoke/*
scripts/integrations/gree-alice/run-local-yandex-provider-smoke.ps1
tests/AssistantEngineer.Tests/GreeAlice/GreeAliceLocalBridgeHttpSmokeBoundaryTests.cs
docs/integrations/gree-alice/local-bridge-http-smoke-boundary.md
docs/integrations/gree-alice/local-bridge-operator-runbook.md
docs/integrations/gree-alice/local-bridge-operator-smoke-checklist.md
docs/integrations/gree-alice/local-bridge-smoke-evidence-template.md
docs/integrations/gree-alice/local-bridge-forbidden-commands.md
docs/integrations/gree-alice/local-yandex-provider-smoke-expectations.md
docs/integrations/gree-alice/README.md
docs/architecture/scripts-tools-inventory.*
PROJECT_STATE.md
```

### Current blocker

```text
None for local GREE-ALICE-52 validation.
Live Gree control remains blocked.
```

### Next step

```text
GREE-ALICE-53 — add Yandex OAuth offline contract skeleton
```

GREE-ALICE-53 may add Yandex OAuth offline contract skeleton only. It should still not implement real OAuth, real provider registration, live Gree+ control, MQTT CONNECT, SUBSCRIBE, PUBLISH, device control, production runtime wiring, deployment changes, or migrations without a separate explicit approval.
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
