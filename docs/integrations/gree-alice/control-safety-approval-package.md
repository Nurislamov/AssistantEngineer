# GREE-ALICE-43: control safety approval package

## Purpose

This document defines a formal control safety approval package for a possible future device-control stage.

It is not a control implementation stage. It adds no live device control, no live Gree+ Cloud control calls, no MQTT calls, no credentials, no real account identifiers, no production runtime wiring, no deployment changes, and no migrations.

## Current status

The bridge remains isolated and offline. `/action` and the control adapter remain dry-run fail-closed.

## Out of scope

This package does not approve or implement control. It does not add live HTTP control calls, MQTT, Gree+ runtime control, real account/device identifiers, production runtime wiring, production deployment wiring, runtime configuration, or migrations.

## Default decision

```text
Control approval status: NOT APPROVED
Live control: disabled
Control adapter: disabled / fail-closed
MQTT: blocked
Production wiring: blocked
Single-device pilot: not approved
```

## Control operations under future review

Candidate list only. This list is not permission to execute commands:

```text
power_on_off
set_mode
set_target_temperature
set_fan_speed
set_swing_vertical
set_swing_horizontal
```

## Forbidden operations

The following remain forbidden until a separate explicit safety approval stage:

```text
firmware_update
device_binding
device_unbinding
account_mutation
schedule_mutation
timer_mutation
scene_execution
bulk_control
multi-device control without approval
vrf-gateway-wide control without approval
child-unit control without explicit child scope
any command without audit event
any command without rollback plan
any command when kill-switch is active
MQTT CONNECT
MQTT SUBSCRIBE
MQTT PUBLISH
production deployment wiring
```

## Device scope requirements

Any future control pilot must be limited to one exact operator-approved test account and one exact operator-approved test device.

Real account identifiers, device identifiers, MAC addresses, and device keys must not be committed. Evidence in the repository must use masked labels only.

## Command limits

No command can be considered without an approved command list, an audit event, a kill-switch plan, a rollback plan, and rate limits.

## Temperature limits

Future approval limits:

```text
Minimum target temperature C: 18
Maximum target temperature C: 30
```

These limits are approval prerequisites only and do not enable control.

## Mode limits

Future allowed mode candidate list:

```text
auto
cool
heat
dry
fan
```

This is a candidate list only and does not enable mode changes.

## Fan and swing limits

Future allowed fan speed candidate list:

```text
auto
low
medium
high
```

Swing changes require explicit approval.

Power off requires explicit approval.

Power on requires explicit approval.

## Rate limits

Rate limit required before live pilot.

Single-device scope required before live pilot.

## Audit and logging requirements

Every future command must produce an audit event before any pilot can be considered. Audit events must use masked account/device identifiers and must not log raw credentials, tokens, passwords, keys, MAC addresses, or raw cloud payloads.

## Kill-switch requirements

A documented kill switch is required before any future control pilot. Any command when the kill switch is active is forbidden.

## Rollback requirements

A documented rollback plan is required before any future control pilot. Rollback must return the bridge to offline fail-closed behavior without production deployment or migration rollback.

## Operator approval requirements

Manual operator approval must be explicit, written, and limited to the exact test account, exact test device, exact command list, exact command limits, rate limits, audit format, kill-switch plan, and rollback plan.

## Evidence requirements

Required evidence before any later approval:

```text
repository clean and synced
all tests pass
bridge remains isolated
live read-only pilot approved separately
control adapter remains fail-closed until explicit approval
MQTT remains blocked
no production deployment wiring
no secrets in repo
credentials stored only outside repo
device/account identifiers masked in evidence
exact single test account approved
exact single test device approved
exact command list approved
temperature, mode, fan, swing, and rate limits approved
audit event format approved
kill-switch plan documented
rollback plan documented
manual operator approval recorded
```

## Validation checklist

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
git diff --stat
```

Validation must confirm no live Gree+ Cloud control code, no MQTT code, no device control code, no production runtime wiring, no production deployment wiring, and no migrations.

## Next stage

The next proposed stage is:

```text
GREE-ALICE-44 — add single-device control pilot skeleton
```

That stage may add a skeleton only. It must still not enable live control without a separate explicit approval.
