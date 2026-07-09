# GREE-ALICE-41: live read-only Gree+ adapter proposal

## Purpose

This document is a formal proposal package for a possible future live read-only Gree+ adapter.

It is not a live adapter implementation. This stage adds no live Gree+ Cloud calls, no live HTTP calls, no MQTT calls, no credentials, no real account identifiers, no device control, no production runtime wiring, no deployment, and no migrations.

## Current status

The bridge remains isolated in `AssistantEngineer.GreeAliceBridge.Api` and runs offline fixture behavior only.

Current implementation status:

```text
Live read-only adapter implemented: no
Live read-only adapter enabled: no
Live read-only pilot approved: no
Live control allowed: no
MQTT allowed: no
Production wiring allowed: no
Approval status: NOT APPROVED
```

## Out of scope

This proposal does not implement live network access, live Gree+ Cloud authentication, MQTT, device control, production deployment wiring, runtime production configuration, or migrations.

It does not change `AssistantEngineer.Api`, Telegram runtime, production deployment scripts, or any production stack.

## Allowed read-only scope

A future live read-only adapter may read only the minimum state needed for Yandex Smart Home state reporting:

```text
device list / device descriptor
online/offline state
power state
mode
target temperature
current temperature
fan speed
swing state
error/status code if exposed by read-only state
```

The adapter must not read broader account profile data, raw credential material, billing data, location data beyond masked home/room labels needed for display, logs unrelated to the selected device state, or any mutable control endpoint.

## Forbidden scope

The following remain forbidden until a separate explicit safety approval stage:

```text
device control
mode changes
temperature changes
power on/off
fan speed changes
swing changes
scene execution
timers/schedules changes
firmware/update actions
account mutations
device binding/unbinding
credential storage in repo
raw token logging
MAC/account/device real identifiers in docs/tests
MQTT CONNECT
MQTT SUBSCRIBE
MQTT PUBLISH
production runtime wiring
deployment changes
migrations
```

## Data handling rules

Only normalized read-only state may cross into bridge responses. Evidence, docs, and tests must use masked values only.

Real account identifiers, real device identifiers, MAC addresses, raw payloads, PCAP, CSV exports, screenshots with identifiers, and unmasked logs must not be committed.

Any future evidence must identify devices with stable masked labels such as `masked-device-001`, and must redact account, home, room, and network identifiers.

## Credential handling rules

Credentials must not be stored in the repository, documentation, tests, fixtures, launch settings, runtime config committed to git, or logs.

Any future live pilot credentials must be provided outside the repository by the operator, scoped to the exact pilot account/device, and revocable before and after the test.

Raw token logging is forbidden.

## Safety gates before live pilot

A future live read-only pilot cannot start unless all gates pass:

```text
Repository clean and synced.
All tests pass.
Bridge remains isolated.
Control adapter remains blocked.
MQTT CONNECT/SUBSCRIBE/PUBLISH remain blocked.
No production deployment wiring.
No secrets in repo.
Credentials stored only outside repo.
Device/account identifiers masked in evidence.
Operator approves exact test account/device.
Kill-switch plan documented.
Rollback plan documented.
Pilot limited to read-only state.
```

## Required operator approval

Approval must be explicit, written, and scoped to one live read-only pilot.

The default status is:

```text
Approval status: NOT APPROVED
```

Without a later approval stage, live read-only adapter code, credentials, live HTTP calls, MQTT operations, and production wiring remain blocked.

## Required evidence before live pilot

Before any live read-only pilot, the project must have masked evidence for:

```text
exact operator-approved test account/device scope
credential storage outside repo
read-only endpoint or API path used for state reads
allowed fields returned by the read-only path
absence of control, mutation, schedule, scene, firmware, binding, and account mutation calls
kill-switch behavior
rollback path
test command set
expected logs with masked identifiers
```

The evidence must show that only read-only state is requested and that control remains blocked.

## Rollback and kill-switch plan

Any future live pilot must have a kill switch that disables the live read-only adapter and returns the bridge to offline fixture behavior.

Rollback must require no production deployment changes and no database migration rollback. Removing external credentials must immediately stop live reads.

Control and MQTT kill switches must remain blocked throughout the pilot.

## Validation checklist

Before the next stage:

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
git diff --stat
```

Validation must confirm that this proposal adds no live Gree+ Cloud code, no MQTT code, no device control code, no production runtime wiring, no production deployment wiring, and no migrations.

## Next stage

The next proposed stage is:

```text
GREE-ALICE-42 — add live read-only pilot gate
```

That stage may add a gate for future live read-only pilot approval. It must still not implement live control, MQTT CONNECT, MQTT SUBSCRIBE, MQTT PUBLISH, production runtime wiring, deployment changes, or migrations.
