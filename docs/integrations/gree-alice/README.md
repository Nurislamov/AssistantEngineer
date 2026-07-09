# GREE-ALICE Bridge

## Goal

Build a Gree-to-Alice integration inside AssistantEngineer that allows users to control Gree split AC and Gree VRF/GMV systems through Yandex Smart Home / Alice without a local server on the object.

## Target architecture

```text
Alice / Yandex Smart Home
        ↓
GreeAliceBridge on VPS
        ↓
Gree Cloud / Gree+
        ↓
Gree split AC and Gree VRF / GMV
```

## Fixed requirements

1. Control through Alice and the Yandex Smart Home app.
2. No required local server or local agent on the object.
3. Devices must be connected to Gree+ / Gree Cloud.
4. Support both split AC units and VRF systems.
5. Keep the implementation inside the AssistantEngineer monorepo, but as a separate deployable service / bounded context.

## Primary path

The primary path is cloud-based integration:

```text
Yandex Smart Home API → VPS bridge → Gree Cloud REST/MQTT → Gree devices
```

Local Gree Wi-Fi UDP control, local agent, VPN, Modbus/BACnet gateway, or BMS integration are fallback paths only.

## Placement decision

GreeAliceBridge must not be mixed into the current production `AssistantEngineer.Api` or Telegram bot runtime.

Recommended future layout:

```text
src/
  Integrations/
    GreeAliceBridge/
      AssistantEngineer.GreeAliceBridge.Api/
      AssistantEngineer.GreeAliceBridge.Application/
      AssistantEngineer.GreeAliceBridge.Domain/
      AssistantEngineer.GreeAliceBridge.Infrastructure/

tests/
  Integrations/
    AssistantEngineer.GreeAliceBridge.Tests/
```

## Protected areas

The first stages must not change these areas unless a later stage explicitly requires it:

```text
src/Backend/AssistantEngineer.Api/
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/
src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations/
data/equipment-diagnostics/
deploy/
scripts/deployment/
```

## Planned stages

### GREE-ALICE-00 — Architecture placement

Document the goal, constraints, safe placement, protected areas, and stage order.

### GREE-ALICE-01 — Open-source research checkpoint

Capture the relevant findings from Gree Wi-Fi, Gree Cloud, MQTT, and Yandex Smart Home research.

### GREE-ALICE-02 — Gree Cloud API probe scaffold

Create a safe probe tool skeleton without connecting it to production runtime.

### GREE-ALICE-03 — Gree+ account and region validation

Validate region selection and basic Gree+ login / account discovery.

### GREE-ALICE-04 — Gree device discovery

List homes, rooms, split AC devices, VRF gateways, child units, MAC/key/model/online status where available.

### GREE-ALICE-05 — Gree Cloud command proof

Test cloud-side control commands for power, mode, temperature, and fan speed.

### GREE-ALICE-06 — VRF child-unit proof

Verify whether Gree Cloud can address individual VRF indoor units behind a G-Cloud gateway.

### GREE-ALICE-07 — State feedback proof

Verify real state feedback after Alice/Gree+ app/remote control changes.

### GREE-ALICE-08 — Bridge service skeleton

Create a separate deployable `GreeAliceBridge.Api` service inside the monorepo.

### GREE-ALICE-09 — Yandex Smart Home MVP

Implement `/v1.0/user/devices`, `/query`, `/action`, and `/unlink` for one test device.

### GREE-ALICE-10+ — Production hardening

Add account security, token encryption, retry behavior, MQTT reconnects, logs, health checks, deploy scripts, and documentation.

## Validation baseline before starting

Before GREE-ALICE work begins, the repository should be on current `origin/master`, with backend tests passing.

Current expected baseline:

```text
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln
dotnet test .\AssistantEngineer.sln
```

## Current status

GREE-ALICE-00 is a documentation-only placement stage.

No runtime code, migrations, Telegram handlers, diagnostics catalog, production deploy files, or Yandex/Gree credentials are added in this stage.

## Current investigation

- [Live/control channel investigation](./live-control-channel-investigation.md)

# GREE-ALICE integration notes

This folder contains safe, isolated documentation for the GREE-ALICE workstream.

The workstream is not connected to AssistantEngineer.Api, Telegram, runtime configuration, deployment, or database migrations.

<!-- GREE-ALICE-DOC-INDEX:START -->
## Current safe review documents

```text
mqtt-connect-readiness-gate.md
mqtt-connect-human-safety-review-checklist.md
mqtt-connect-safety-review-decision-record.md
mqtt-connect-operator-sign-off-template.md
mqtt-connect-offline-review-packet-summary.md
mqtt-live-gate-fail-closed-policy.md
mqtt-connect-future-live-probe-boundary.md
offline-bridge-path-decision.md
yandex-smart-home-offline-endpoint-contract.md
offline-fixture-model-boundary.md
offline-bridge-project-skeleton.md
yandex-smart-home-offline-api-skeleton.md
yandex-smart-home-offline-api-contract-hardening.md
offline-account-device-registry-boundary.md
gree-cloud-adapter-interface-boundary.md
read-only-cloud-state-mapping-contract.md
bridge-safety-middleware-and-kill-switches.md
offline-end-to-end-bridge-flow.md
isolated-staging-deploy-skeleton.md
```

Current safety position:

```text
Chosen path: offline Yandex Smart Home bridge skeleton first
Runtime mode: offline-fixture
HTTP endpoint implementation: offline API skeleton only
Fixture endpoints: enabled
/action mode: dry-run fail-closed
Offline API contract: hardened
Unknown device behavior: offline unknown state or fail-closed action result
Unknown capability behavior: fail-closed action result
Bad/empty request behavior: controlled offline error response where model binding succeeds
Registry boundary: offline fixture only
Registry mode: offline-fixture-registry
Registry device kinds: split-ac, vrf-gateway, vrf-child-indoor-unit, unknown
Gree Cloud adapter boundary: offline-fake only
Gree Cloud read adapter: offline registry only
Gree Cloud control adapter: dry-run fail-closed
Read-only state mapping: offline masked fixture only
State mapping mode: offline-masked-fixture
Missing/unknown state fields: controlled mapping issues
Bridge safety layer: offline safety decision service
Control adapter kill switch: enabled
Action safety: dry-run fail-closed only
Offline E2E flow tests: health, devices, query, action, unlink
Isolated staging deploy skeleton: docs-only, offline-only, no production wiring
Live Gree+ Cloud calls: blocked
Live CONNECT: blocked
SUBSCRIBE: blocked
PUBLISH: blocked
Device control: blocked
Runtime integration: blocked
Gree+ runtime control: blocked
Deployment changes: none
Migrations: none
```
<!-- GREE-ALICE-DOC-INDEX:END -->
