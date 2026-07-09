# GREE-ALICE-44: single-device control pilot skeleton

## Purpose

This document defines a skeleton for a possible future single-device control pilot.

It is not a control enablement stage. It adds no live device control, no live Gree+ Cloud control calls, no MQTT calls, no credentials, no real account identifiers, no production runtime wiring, no deployment changes, and no migrations.

## Current status

The bridge remains isolated and offline. `/action` remains an offline dry-run/fail-closed response.

## Out of scope

This skeleton does not approve or run commands. It does not add live HTTP control calls, MQTT, Gree+ runtime control, actual command execution, real account/device identifiers, production runtime wiring, production deployment wiring, runtime configuration, or migrations.

## Pilot status

```text
Pilot status: NOT APPROVED
Live control: disabled
Command sending: disabled
Control adapter: fail-closed
MQTT: blocked
Production wiring: blocked
```

## Pilot scope model

The skeleton scope is dummy/offline only:

```text
Pilot account id: dummy-account-001
Pilot device id: dummy-gree-ac-001
Pilot device kind: split-ac
Pilot scope kind: single-device-offline-fixture
```

No real account id, real device id, MAC address, device key, credential, token, password, or Gree+ user data is allowed in repository docs or tests.

## Allowed candidate commands

Candidate list only. This list is not approval to send commands:

```text
power_on_off
set_mode
set_target_temperature
set_fan_speed
set_swing_vertical
set_swing_horizontal
```

Each candidate remains:

```text
IsApproved: false
DryRunOnly: true
WillSendToGreeCloud: false
WillSendToMqtt: false
WillSendToDevice: false
RequiresManualApproval: true
RequiresAuditEvent: true
RequiresKillSwitchClear: true
```

## Forbidden commands

All live control, MQTT CONNECT, MQTT SUBSCRIBE, MQTT PUBLISH, production wiring, deployment wiring, bulk control, account mutation, schedule mutation, scene execution, firmware update, device binding, and device unbinding remain forbidden.

## Dry-run execution path

Yandex `/action` remains accepted only as an offline dry-run/fail-closed response.

No Gree+ command is sent.

No MQTT is used.

No device receives a command.

## Fail-closed behavior

The control adapter remains disabled/fail-closed. Any command plan result must remain dry-run only and must report:

```text
SentToGreeCloud: false
SentToMqtt: false
SentToDevice: false
```

## Safety prerequisites

Future approval prerequisites:

```text
MinTargetTemperatureC: 18
MaxTargetTemperatureC: 30
AllowedModesCandidate: auto, cool, heat, dry, fan
AllowedFanSpeedsCandidate: auto, low, medium, high
RateLimitRequired: true
SingleDeviceScopeRequired: true
AuditEventRequired: true
KillSwitchRequired: true
RollbackRequired: true
```

These limits do not enable live control.

## Audit event requirements

Any future command must have an approved audit event format before a pilot can be considered. Audit data must use masked account/device labels and must not include credentials, tokens, passwords, device keys, MAC addresses, or raw cloud payloads.

## Kill-switch requirements

A kill switch is required before any future control pilot. If the kill switch is active, every command remains blocked.

## Rollback requirements

Rollback must return the bridge to offline fail-closed behavior without production deployment or migration rollback.

## Validation checklist

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
git diff --stat
```

Validation must confirm no live Gree+ Cloud control code, no MQTT code, no device control code, no actual command execution, no production runtime wiring, no production deployment wiring, and no migrations.

## Next stage

The next proposed stage is:

```text
GREE-ALICE-45 — add minimal production pilot boundary
```

That stage may add a minimal boundary only. It must still not enable live control without separate explicit approval.
