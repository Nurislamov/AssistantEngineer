# AssistantEngineer Project State

<!-- GREE-ALICE-STATE:START -->
## GREE-ALICE current checkpoint

### Current stage

GREE-ALICE-19 — APPLIED locally / validation pending.

Latest pushed commit:

```text
f2595145 GREE-ALICE-16 Add MQTT evidence gate decision
```

Current local stage prepared for validation:

```text
GREE-ALICE-19 Add CONNECT-only dry-run command contract
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
```

### Validation status

```text
Latest full validation before GREE-ALICE-17:
dotnet test .\AssistantEngineer.sln --no-build
Result: PASS
Tests: 5411/5411
```

### Current Gree+ Cloud facts

```text
Gree+ Cloud login: PASS
Validated REST server: https://hkgrih.gree.com
MQTT/TLS endpoint candidate: mqtt-hk.gree.com:1994
MQTT/TLS endpoint TLS probe: PASS
Real control-action capture: PASS
Action sequence: off/on and setpoint 24 -> 23 -> 24
MQTT/TLS control candidate observed during action: yes
REST discovery traffic observed during action: yes
UDP 7000 LAN activity observed during action: yes
```

### GREE-ALICE-15 findings

```text
Masked local artifacts scanned: 25
JSON files parsed: 25
Distinct field names: 240
Sensitive/identity field name hits: 124
MQTT signal field name hits: 206
Raw leak candidate hits: 10
Output contains raw values: no
Network connection opened: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
```

### GREE-ALICE-16 findings

```text
Inventory report found: yes
Files scanned: 25
JSON files parsed: 25
Distinct field names: 240
Sensitive/identity field name hits: 124
MQTT signal field name hits: 206
Raw leak candidate hits: 10
Client id field-name signal: no
Username field-name signal: yes
Auth field-name signal: yes
Topic field-name signal: yes
Decision: blocked-evidence-incomplete
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
Blockers: 4
```

Blockers:

```text
No client id field-name signal was found.
Field-name signals are not enough for MQTT CONNECT.
Raw client id, username, auth secret, and topic values remain unknown.
SUBSCRIBE, PUBLISH, and device control remain blocked even if CONNECT-only is later approved.
```

### GREE-ALICE-17 local scope

```text
Documentation-only safety specification for possible future MQTT CONNECT-only probe.
No MQTT CONNECT implementation.
No TCP/TLS/MQTT network connection.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No production bridge.
No API/Telegram/runtime/deployment/migration changes.
No third-party source names in docs.
```

### GREE-ALICE-18 local scope

```text
Adds repository guard tests for MQTT CONNECT-only input contract and safety docs.
Tests fail if docs contain forbidden third-party source references.
Tests fail if offline validation/evidence commands include live MQTT/network implementation markers.
Tests fail if CONNECT-only safety spec no longer keeps SUBSCRIBE/PUBLISH/control blocked.
No MQTT CONNECT implementation.
No TCP/TLS/MQTT network connection.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
```
### GREE-ALICE-19 local scope

```text
Adds offline dry-run command for future MQTT CONNECT-only input contract.
Validates missing/invalid/unsafe inputs.
Rejects topic/payload/control arguments.
Writes masked report only.
No MQTT CONNECT implementation.
No TCP/TLS/MQTT network connection.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
```
### Important decisions

```text
Keep GREE-ALICE isolated in tools/AssistantEngineer.Tools.GreeCloudProbe for now.
Do not wire into AssistantEngineer.Api, Telegram, deployment, migrations, or runtime config yet.
Do not commit local artifacts under artifacts/gree-alice/.
Do not commit private PCAP/CSV exports.
Do not store Gree+ credentials in files or repository.
Use masked reports only.
Control commands are still out of scope until live-status/control channel auth/topic model is confirmed.
MQTT investigation must remain read-only until the protocol, auth model, topics, and payload safety are understood.
Do not start MQTT CONNECT/SUBSCRIBE/PUBLISH/control work without an explicit safety stage.
Future MQTT CONNECT implementation is blocked until client id/auth inputs are known and guard rails are documented.
Internal vocabulary candidates are not proof of Gree+ Cloud MQTT auth, topic, QoS, or payload envelope.
Do not mention third-party protocol sources in GREE-ALICE project docs unless explicitly requested later.
```

### Files changed recently

```text
tools/AssistantEngineer.Tools.GreeCloudProbe/Program.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/README.md
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttEvidenceInventoryCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttEvidenceGateDecisionCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectDryRunCommand.cs
docs/integrations/gree-alice/mqtt-auth-topic-evidence-plan.md
docs/integrations/gree-alice/mqtt-evidence-inventory.md
docs/integrations/gree-alice/mqtt-evidence-gate-decision.md
docs/integrations/gree-alice/mqtt-connect-only-safety-specification.md
docs/integrations/gree-alice/mqtt-connect-input-contract-tests.md
docs/integrations/gree-alice/mqtt-connect-dry-run-contract.md
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectInputContractSafetyTests.cs
PROJECT_STATE.md
```

### Next step

Validate and commit GREE-ALICE-17 docs-only stage.

Recommended validation:

```text
git diff --check
dotnet build .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj --no-restore
dotnet test .\AssistantEngineer.sln --no-build
```

Recommended commit:

```text
GREE-ALICE-19 Add CONNECT-only dry-run command contract
```

After GREE-ALICE-17 is pushed, the next possible stage is:

```text
GREE-ALICE-19 — CONNECT-only dry-run command contract
```

GREE-ALICE-19 should define a dry-run command contract only. It should still not implement live MQTT CONNECT.
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
