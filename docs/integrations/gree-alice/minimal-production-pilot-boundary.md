# GREE-ALICE-45: minimal production pilot boundary

## Purpose

This document defines a formal minimal production pilot boundary for a possible future limited production pilot.

It is not a production deploy stage, not a live control stage, and not a Gree+ Cloud runtime enablement stage.

## Current status

The bridge remains isolated, offline, and fail-closed. Production pilot start, production wiring, live Gree+ runtime, MQTT, and control remain blocked.

## Out of scope

This stage does not add live HTTP calls, MQTT calls, credentials, real account/device identifiers, production deployment wiring, runtime secret/env configuration, migrations, live adapter implementation, live control implementation, actual command execution, or production pilot start endpoint.

## Default decision

```text
Minimal production pilot status: NOT APPROVED
Production deployment wiring: disabled
Live Gree+ Cloud runtime: disabled
Live control: disabled
MQTT: blocked
Secrets in repository: forbidden
Pilot rollout: blocked by default
```

## Pilot scope

A future minimal pilot scope must be limited to:

```text
single operator
single account
single home
single device or explicitly scoped VRF child unit
read-only-first
manual rollback
manual kill-switch
masked evidence only
no bulk rollout
no automatic discovery rollout
no multi-account rollout
no gateway-wide VRF control
```

These rules are policy only and do not enable a pilot.

## Read-only-first rule

Any minimal production pilot must start read-only.

Control requires a separate approval package and a separate explicit stage.

Read-only pilot approval does not imply control approval.

Control approval does not imply production rollout approval.

## Control policy

Live control remains disabled. Command sending remains disabled. The control adapter remains fail-closed.

## Allowed pilot shape

The only future pilot shape that can be reviewed is a manually approved, read-only-first pilot for one operator, one account, one home, and one exact device or explicitly scoped VRF child unit.

## Forbidden rollout shape

Bulk rollout, automatic discovery rollout, multi-account rollout, gateway-wide VRF control, production control rollout, MQTT rollout, and production deployment wiring are forbidden.

## Data and credential rules

Secrets must not be stored in the repository.

Credentials must be stored only outside the repository.

Real account, home, device, operator, and VRF child-unit identifiers must be masked in repository docs/tests and evidence references.

No MAC addresses, device keys, tokens, passwords, PCAP, CSV, screenshots with identifiers, raw logs, or raw cloud payloads may be committed.

## Operator checklist

The manual checklist is `minimal-production-pilot-checklist.md`. Its default status is `NOT APPROVED`.

## Audit requirements

Any future pilot must have an approved audit event format before live review. Audit records must use masked identifiers and must not log secrets.

## Kill-switch requirements

A manual kill-switch plan is required before any pilot. When the kill switch is active, the pilot remains blocked.

## Rollback requirements

A manual rollback plan is required before any pilot. Rollback must not require production migration rollback.

## Monitoring requirements

A monitoring plan must be documented before any pilot. Monitoring must cover bridge health, read-only request results, error rates, and operator-visible stop conditions.

## Production wiring policy

No production `AssistantEngineer.Api` wiring in this stage.

No Telegram runtime wiring in this stage.

No production deployment scripts changed in this stage.

No migrations in this stage.

No runtime secret/env config in this stage.

## Validation checklist

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
git diff --stat
```

Validation must confirm no live Gree+ Cloud code, no MQTT code, no device control code, no production runtime wiring, no production deployment wiring, and no migrations.

## Next stage

The next proposed stage is:

```text
GREE-ALICE-46 — add VRF/GMV child-unit support
```

That stage may add VRF/GMV child-unit support only within the current safety boundaries unless a separate explicit approval changes the boundary.
