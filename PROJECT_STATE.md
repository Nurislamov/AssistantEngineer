# AssistantEngineer Project State

<!-- GREE-ALICE-STATE:START -->
## GREE-ALICE current checkpoint

### Current stage

GREE-ALICE-12 — CLOSED / pushed.

Latest commit:

```text
7968141c GREE-ALICE-12 Add MQTT CONNECT input validation scaffold
```

State checkpoint to commit next:

```text
GREE-ALICE-12.STATE — Update project state after MQTT input validation scaffold
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
```

### Validation status

```text
dotnet test .\AssistantEngineer.sln --no-build
Result: PASS
Tests: 5411/5411
```

### Cloud validation

```text
Gree+ Cloud login: PASS
Region shown by app/account: Ouzbekistan / Ouzbékistan
Validated REST server: https://hkgrih.gree.com
Homes: 1
Rooms: 1
Devices: 1
Device: AC3167
Device version: V3.4.M
Device key: provided
Sensitive fields: masked in reports
```

### REST discovery path

```text
/App/UserLoginV2: PASS
/App/GetHomes: PASS
/App/GetDevsInRoomsOfHomeV2: PASS
```

### Device classification note

```text
First cloud-visible device: cloud room climate candidate
Do not treat it as proven VRF control until parent/child or gateway fields are confirmed.
```

### GREE-ALICE-06 findings

```text
Read-only live status probe tool: implemented
Live probe login/discovery: PASS
Candidate REST endpoints attempted: 32
Result: all attempted /App/Get... live-status endpoints returned HTTP 404
Live capability fields found: none
Missing fields: Pow / Mod / SetTem / WdSpd / temperature / fan / swing
```

Conclusion:

```text
GetDevsInRoomsOfHomeV2 provides metadata only.
Simple REST /App/GetDeviceStatus-style endpoints are not the live status/control channel on hkgrih.gree.com.
```

### GREE-ALICE-07 findings

```text
Private GREE+ app traffic export indicated:
- hkgrih.gree.com:443 as HTTPS REST discovery path
- mqtt-hk.gree.com:1994 as MQTT/TLS live channel candidate
- 255.255.255.255:7000 as local UDP discovery fallback
```

The CSV / PCAP export itself is private diagnostic material and must not be committed.

### GREE-ALICE-08 findings

```text
Read-only MQTT channel probe: implemented
Target: mqtt-hk.gree.com:1994
DNS resolved: PASS
TCP connected: PASS
TLS/SNI authenticated: PASS
TLS protocol: Tls12
Certificate subject: CN=*.gree.com, O=珠海格力电器股份有限公司, L=珠海市, S=广东省, C=CN
Certificate issuer: CN=GlobalSign RSA OV SSL CA 2018, O=GlobalSign nv-sa, C=BE
Certificate not after: 10.04.2027 14:35:22
Resolved addresses: 18.139.13.162, 54.254.105.150
```

Safety result:

```text
MQTT application data sent: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Control command sent: no
```

### GREE-ALICE-09 findings

```text
Offline MQTT auth/topic model draft: implemented
Discovery report found: yes
MQTT channel report found: yes
Transport host: mqtt-hk.gree.com
Transport port: 1994
TLS authenticated: yes
TLS protocol: Tls12
Device signals: 1
Device key presence count: 1
Masked MAC signal count: 1
Auth model status: unknown-read-only
Topic model status: unknown-read-only
```

Safety result:

```text
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
Raw device keys stored: no
Raw MACs stored: no
```

Known unknowns after GREE-ALICE-09:

```text
MQTT client id format
MQTT username format
MQTT password/token format
Whether MQTT auth uses cloud uid/token, device mac/key, app token, region secret, or another signed payload
Status topic naming
Command topic naming
Account/home/device topic prefixes
QoS level
Payload encryption/signature shape
Whether status is pushed only after subscription
```

### GREE-ALICE-10 findings

```text
Offline MQTT CONNECT-only safety review: implemented
Model report found: yes
TLS authenticated: yes
Device signals: 1
Device key presence count: 1
Masked MAC signal count: 1
Auth model status: unknown-read-only
Topic model status: unknown-read-only
Decision: not-ready
Blockers: 7
```

Blockers:

```text
MQTT auth model is not known. Current status must remain unknown-read-only.
MQTT topic model is not known. This blocks subscribe/publish/control work.
MQTT client id format is not confirmed.
MQTT username format is not confirmed.
MQTT password/token format is not confirmed.
CONNECT-only packet shape is not specified.
CONNACK handling and immediate DISCONNECT behavior are not implemented.
```

Safety result:

```text
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
Production bridge changed: no
```

### GREE-ALICE-11 findings

```text
Offline MQTT CONNECT input contract: implemented
Contract status: draft-connect-still-blocked
Inputs: 12
Required future inputs: 3
Secret inputs: 4
Blockers: 7
```

Reserved future environment variables:

```text
GREE_ALICE_MQTT_HOST
GREE_ALICE_MQTT_PORT
GREE_ALICE_MQTT_CLIENT_ID
GREE_ALICE_MQTT_USERNAME
GREE_ALICE_MQTT_PASSWORD
GREE_ALICE_MQTT_TOKEN
GREE_ALICE_MQTT_AUTH_MODE
GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS
GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS
GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK
GREE_ALICE_MQTT_ALLOW_SUBSCRIBE
GREE_ALICE_MQTT_ALLOW_PUBLISH
```

Safety result:

```text
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
```

### GREE-ALICE-12 findings

```text
Offline MQTT CONNECT input validation scaffold: implemented
Validation status with no env vars: blocked-fail-closed
Inputs checked: 12
Provided inputs: 0
Missing required inputs: 3
Invalid inputs: 3
Unsafe inputs present: 0
Violations: 6
```

Missing required future inputs:

```text
GREE_ALICE_MQTT_CLIENT_ID
GREE_ALICE_MQTT_USERNAME
GREE_ALICE_MQTT_AUTH_MODE
```

Safety result:

```text
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
No TCP/TLS/MQTT network connection opened
```

### Latest private capture evidence

New PCAPdroid export captured real Gree+ control actions:

```text
Action sequence: off/on and setpoint 24 -> 23 -> 24
App: GREE+
Package: com.gree.greeplus
Capture file: PCAPdroid_08_июл._15_58_15.csv
```

Important private finding to document in the next stage:

```text
During the control action window, Gree+ used mqtt-hk.gree.com:1994 over TLS.
The same capture also showed HTTPS traffic to hkgrih.gree.com:443 and local UDP 7000 activity.
The CSV itself is private diagnostic material and must not be committed.
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
Future MQTT CONNECT input validation must fail closed and must not open a MQTT connection.
Open-source Gree LAN protocol projects may inform payload names, but they do not by themselves prove Gree+ Cloud MQTT auth/topics.
```

### Files changed recently

```text
tools/AssistantEngineer.Tools.GreeCloudProbe/Program.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/README.md
tools/AssistantEngineer.Tools.GreeCloudProbe/LiveStatusProbeCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttChannelProbeCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttModelDraftCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectSafetyReviewCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectInputContractCommand.cs
tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectInputValidationCommand.cs
docs/integrations/gree-alice/live-control-channel-investigation.md
docs/integrations/gree-alice/mqtt-channel-handshake.md
docs/integrations/gree-alice/mqtt-auth-topic-model.md
docs/integrations/gree-alice/mqtt-connect-safety-review.md
docs/integrations/gree-alice/mqtt-connect-input-contract.md
docs/integrations/gree-alice/mqtt-connect-input-validation.md
PROJECT_STATE.md
```

### Next step

GREE-ALICE-13 — Control action capture and reference evidence.

Planned scope:

```text
- document the new Gree+ control-action capture in masked form;
- document that mqtt-hk.gree.com:1994 TLS traffic is active during off/on and setpoint changes;
- document parallel hkgrih.gree.com:443 HTTPS and UDP 7000 LAN activity;
- review open-source Gree protocol references as inspiration only;
- separate LAN UDP/7000 protocol evidence from Gree+ Cloud MQTT evidence;
- identify which payload names are reusable as model vocabulary: Pow, Mod, SetTem, WdSpd, swing fields;
- explicitly state what remains unknown: MQTT client id, username, password/token, topics, QoS, payload encryption/signature;
- no MQTT CONNECT implementation yet;
- no TCP/TLS/MQTT network connection;
- no MQTT SUBSCRIBE;
- no MQTT PUBLISH;
- no wildcard topics;
- no device control payloads;
- no production bridge;
- no private capture files committed;
- keep all outputs masked and local under artifacts/.
```
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
