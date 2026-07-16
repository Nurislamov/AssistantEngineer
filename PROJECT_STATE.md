# AssistantEngineer Project State

<!-- GREE-ALICE-STATE:START -->
## GREE-ALICE current checkpoint

### Current stage

GREE-ALICE-API-DISCOVERY-DOC1 — documentation-only discovery checkpoint.

GREE-ALICE-PILOT-1B is already present in git history and pushed on `master`; it is no longer only a local validation-pending stage.

Latest closed GREE-ALICE commit before this documentation checkpoint:

```text
4422b240 GREE-ALICE-LIVE-EVIDENCE-1b Harden focused Gree evidence extraction
```

Current documentation stage prepared in this working tree:

```text
GREE-ALICE-API-DISCOVERY-DOC2 Recover Android/ART API discovery history
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
GREE-ALICE-52 — CLOSED / pushed — local bridge HTTP smoke endpoint boundary
GREE-ALICE-53 — CLOSED / pushed — release readiness audit and RC path
GREE-ALICE-RC1 — CLOSED / pushed — internal offline release candidate
GREE-ALICE-PILOT-1A — CLOSED / pushed — Yandex OAuth provider pilot contract
GREE-ALICE-PILOT-1B — CLOSED / pushed — dev-only Yandex OAuth provider vertical slice
GREE-ALICE-LIVE-EVIDENCE-1 — CLOSED / pushed — read-only evidence capture package
GREE-ALICE-LIVE-EVIDENCE-1a — CLOSED / pushed — evidence redaction hardening
GREE-ALICE-LIVE-EVIDENCE-1b — CLOSED / pushed — focused evidence extraction hardening
```

### Validation status

```text
Latest validation for GREE-ALICE-PILOT-1A:
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
Result: PASS
Tests: 5701/5701
git diff --check: PASS
Local smoke script: PASS
Optional HTTP smoke http://localhost:5005: PASS
Push: PASS
master == origin/master after commit 1686adb3
```

Latest factual git state before GREE-ALICE-API-DISCOVERY-DOC1:

```text
Current branch: master
HEAD before DOC1: 4422b240dfc700eb491332777e18dea9d05ec426
origin/master before DOC1: 4422b240dfc700eb491332777e18dea9d05ec426
Working tree before DOC1: clean
git diff --check before DOC1: PASS
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

### GREE-ALICE-PILOT-1B local scope

```text
Implements dev-only/local OAuth-like vertical slice.
Adds GET /oauth/authorize, GET /oauth/callback, POST /oauth/token in isolated bridge API only.
Adds in-memory authorization code and token stores.
Adds Bearer-token provider mode for configured PrivateSkillDevOnly.
Keeps default local/offline provider endpoints compatible without bearer.
Adds smoke script -RunOAuthSmoke for localhost-only dev smoke.
Keeps production OAuth runtime not implemented.
Keeps current Yandex Smart Home production release status as NOT READY.
Sets next implementation stage to GREE-ALICE-PILOT-2.
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
Real production OAuth remains not implemented.
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
Internal/offline engineering release is near-ready.
Yandex Smart Home production release is not ready.
Internal/offline RC1 is cut locally.
Private skill pilot contract is design-only.
Smoke harness does not call real Yandex.
Smoke script does not call real Yandex.
HTTP smoke boundary does not call real Yandex.
Smoke harness does not implement OAuth.
Smoke script supports dev-only local OAuth smoke.
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
Release audit does not add runtime functionality.
Pilot contract exists and PILOT-1B adds dev-only runtime functionality.
Pilot implementation does not add production OAuth endpoints.
Pilot contract does not add real Yandex credentials or tokens.
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

### GREE-ALICE-API-DISCOVERY-DOC1 documentation checkpoint

```text
Stage type: documentation-only
Canonical API contract inventory: created
API contract gaps document: created
Passive gateway capture plan: created
Static APK inventory: PASS
Flutter/plugin extraction: PASS
Package: com.gree.greeplus
Observed app version: 1.25.3.7
Static inventory run: run-20260712-103917
Flutter/plugin run: run-20260712-111123
PluginArtifactCount: 68
PluginJadxSuccessCount: 27
LibAppStringCount: 26950
LibAppFocusedCount: 1921
LibAppEndpointCount: 32
PluginFocusedHitCount: 2529
PluginContractHitCount: 2525
PluginEndpointCount: 235
CombinedContractCount: 4699
AccessActionStaticHits: 1
SendDataToDevicePluginHits: 40
MqttPluginHits: 7
libapp.so SHA-256: 43071232B93304D2F2249DE1CB2EC72D9BDD5F42451EBF1D17CDCA9E70E7A720
Clean static application inventory: 77 /App/*, 8 /Stats/*, 1 /GreeAccess/*, 86 total
Runtime-observed HTTPS candidates: /App/QueryOnline, /App/OptHistory
Runtime-correlated command endpoint candidate: /GreeAccess/access/action
Command/sendDataToDevice evidence: confirmed in plugin/static evidence
Exact auth/method/body/response contract: unknown
Exact read-only proof: unknown
Exact command transport envelope: unknown
Live network/control performed in DOC1: no
MQTT operations performed in DOC1: no
Raw artifacts committed: no
Production/API/Telegram/deployment/migration changes: no
Next evidence stage: GREE-ALICE-GATEWAY-CAPTURE-1 — passive Wi-Fi gateway metadata capture and channel correlation
```

### GREE-ALICE-API-DISCOVERY-DOC2 documentation checkpoint

```text
Stage type: documentation-only
Documentation root: docs/integrations/gree-alice/discovery
Live evidence root inspected: D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY
Production/runtime unchanged: yes
Database unchanged: yes
Migrations added: none
HVAC commands sent: none
Phone/Frida/observer/network operations run by DOC2: none
Raw evidence copied to git: no
Last valid ART/nterp stage: v1.0.48 GREE EXECUTENTERP REGISTER FILTER FEASIBILITY
Last attempted ART/nterp stage: v1.0.49b GREE EXECUTENTERP TARGET ARTMETHOD CORRELATION
v1.0.49b ZIP SHA256: C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC
v1.0.49b result: INVALID
v1.0.49b blocker: SessionDetachedBeforeTargetArtMethodGate before hook-ready/gate-complete
Confirmed late-stage facts: direct JNI 4/4 classes and 8/8 method IDs; slot +16 CodeItem-like; slot +24 shared ExecuteNterpImpl; x0 plausible ArtMethod register candidate
Invalidated assumptions: slot +16 native hook target; direct x0 == jmethodID equality; v1.0.49b as successful deferred-classification proof
Documentation files created: README, DISCOVERY-TIMELINE, CONFIRMED-FINDINGS, FAILED-AND-INVALID-BRANCHES, METHODCHANNEL-ENTRYPOINTS, ART-AND-NTERP-NOTES, LAB-RUNBOOK, EVIDENCE-INDEX, ARCHIVE-REPORT-v1.0.49b, CURRENT-STATE, DECISION-LOG, GLOSSARY, DOCUMENTATION-VALIDATION
Current recovered blocker: target ArtMethod/CodeItem correlation gate fails before hook-ready because the process/session detaches before capture
Recommended next safe branch: fix-first-host-or-agent-error using offline diagnosis of existing v1.0.49a/v1.0.49b host/agent logs before any new device work
```

### Files changed by GREE-ALICE-PILOT-1B

```text
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Api/Program.cs
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Application/YandexSmartHome/OAuth/*
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Contracts/YandexSmartHome/OAuth/*
docs/integrations/gree-alice/yandex-oauth-provider-pilot-contract.md
docs/integrations/gree-alice/yandex-oauth-provider-pilot-config.example.json
docs/integrations/gree-alice/yandex-oauth-provider-dev-smoke.md
docs/integrations/gree-alice/release-readiness-audit.md
docs/integrations/gree-alice/internal-offline-release-notes-draft.md
docs/integrations/gree-alice/README.md
tests/AssistantEngineer.Tests/GreeAlice/GreeAliceYandexOAuthProviderPilotContractTests.cs
tests/AssistantEngineer.Tests/GreeAlice/GreeAliceYandexOAuthProviderPilotVerticalSliceTests.cs
scripts/integrations/gree-alice/run-local-yandex-provider-smoke.ps1
PROJECT_STATE.md
```

### Current blocker

```text
None for GREE-ALICE-API-DISCOVERY-DOC2 documentation.
Live Gree control remains blocked.
Android/ART target correlation remains blocked by v1.0.49b early process/session detach before hook-ready.
```

### Next step

```text
GREE-ALICE-API-DISCOVERY-DOC2-FOLLOWUP — offline diagnosis of v1.0.49a/v1.0.49b first host/agent/process termination
```

GREE-ALICE-API-DISCOVERY-DOC2-FOLLOWUP should use existing local logs only. It must not launch GREE+, connect to the phone, run Frida, run observers, send network requests, send HVAC commands, add live Gree+ control, MQTT CONNECT, SUBSCRIBE, PUBLISH, device control, production runtime wiring in AssistantEngineer.Api, migrations, deployment changes, or secrets without a separate explicit approval.

GREE-ALICE-GATEWAY-CAPTURE-1 remains a possible later passive metadata stage after the Android/ART discovery state is closed or explicitly parked. It should capture passive metadata only: DNS, destination IP, ports, TLS/SNI where visible, packet sizes, timing, and channel correlation.

GREE-ALICE-PILOT-2 remains a possible separate implementation/deployment track after explicit approval, but it is not the next evidence step for the Gree Plus API contract.
<!-- GREE-ALICE-STATE:END -->


## Current stage

ED-24BOT.CORE1 — CLOSED / production PASS.

Bot architecture audit and first behavior-preserving channel-neutral diagnostic core extraction are merged to master and deployed to production.

### ED-24BOT.PARSE1

ED-24BOT.PARSE1 — CLOSED locally / validation PASS.

```text
Telegram diagnostic input accepts Cyrillic visual equivalents for diagnostic-code characters.
Diagnostic code case is preserved; no blanket uppercasing is applied.
Explicit separators -, ., _ are supported for separated two-character diagnostic codes.
Whitespace-separated two-character codes require restricted diagnostic context.
Whitespace-separated context is split into strong and weak forms; every whitespace-separated candidate must end the message.
Ordinary Russian phrases are protected against false-positive parsing from preposition/conjunction + digit pairs.
Ambiguous letter/digit substitutions are not performed: O/0, I/1, L/1, S/5, B/8, Z/2, G/6 remain distinct.
No migrations.
No configuration changes.
No knowledge JSON changes.
Production deployment not performed in this stage.
Focused validation: PASS, 261/261.
dotnet restore .\AssistantEngineer.sln: PASS.
dotnet build .\AssistantEngineer.sln --no-restore: PASS, 0 warnings, 0 errors.
dotnet test .\AssistantEngineer.sln --no-build --logger "console;verbosity=minimal": PASS, 5987/5987.
```

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
