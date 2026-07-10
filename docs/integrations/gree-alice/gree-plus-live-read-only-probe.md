# GREE-ALICE-LIVE-READ-1: Gree Plus live read-only probe scaffold

## Purpose

This stage adds a local scaffold for a possible one-device Gree Plus live state read.

The scaffold is read-only and fail-closed. It is not a production bridge, not an Alice production readiness milestone, and not a device control stage.

## Current status

```text
Live read-only probe scaffold: implemented
Live endpoint/auth contract: not confirmed
Default result: NotReady or blocked
Network call by default: disabled
Device control: disabled
Production wiring: disabled
Alice production readiness: NOT READY
```

The exact authenticated Gree Plus status endpoint and request contract are still missing. The probe must not report live success until that contract is confirmed in a later reviewed stage.

## Required local approval inputs

A live read attempt remains blocked unless all inputs are present:

```text
GREE_ALICE_ENABLE_LIVE_READ=true
explicit --approve-read-only operator flag
one allowlisted device alias
local-only config source under .local or environment-only config source
```

Credentials and device identifiers must be supplied only through untracked local files or environment variables. A possible local path is `.local/gree-plus-live/read-only.local.json`; that file must not be committed.

## Read-only boundary

Allowed scope:

```text
parse an operator-provided read-only status JSON payload
return normalized status through GreePlusDeviceStatusParser
return redacted diagnostics
fail closed when the live read contract is missing
```

Forbidden scope:

```text
device control
SetTem command
Power command
mode/fan/swing/feature command
timers or schedules
MQTT publish
MQTT subscribe
MQTT command/control
live write/action endpoints
production API wiring
Telegram wiring
deployment changes
migrations
checked-in credentials
checked-in MAC/homeId/deviceId/uid/token/credential values
```

The scaffold does not send commands and does not prepare command payloads. It only gates a future read-only path and can parse a supplied status payload offline.

## Redaction rules

Logs, diagnostics, and public results must not expose raw:

```text
token
cookie
authorization header
email
uid
homeId
deviceId
mac
credential
password
secret
```

Evidence for future stages must use masked aliases only, such as `masked-device-001` or a local operator alias.

## Operator checklist for a later live read

Before replacing the fail-closed scaffold with a live read implementation, capture masked evidence for:

```text
exact authenticated read-only status endpoint
required request method and body/query shape
required headers without raw credential values
confirmed absence of control/write/action endpoints
single operator-approved device alias
credential source outside the repository
redacted expected diagnostics
rollback step that removes credentials or disables GREE_ALICE_ENABLE_LIVE_READ
```

If any item is missing, the probe stays `NotReady` and must not attempt the network.

## Next stage

The next stage after a successful reviewed live read is:

```text
GREE-ALICE-LIVE-CONTROL-GATE-1
```

That future stage must still require a separate approval gate before any control behavior is considered.
