# GREE-ALICE MQTT CONNECT-only safety specification

## Stage

`GREE-ALICE-17` defines the safety specification for a possible future MQTT `CONNECT`-only probe.

This stage is documentation-only. It does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This document does not name or cite third-party protocol sources. It is based only on our own masked captures, local masked artifacts, and current gate decisions.

## Current gate status

The latest masked evidence gate decision remains:

```text
Decision: blocked-evidence-incomplete
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

Latest known field-name signals:

```text
Client id field-name signal: no
Username field-name signal: yes
Auth field-name signal: yes
Topic field-name signal: yes
```

Important interpretation:

```text
Field-name signals are not enough for MQTT CONNECT.
Raw client id, username, auth secret, and topic values remain unknown.
SUBSCRIBE, PUBLISH, and device control remain blocked even if CONNECT-only is later approved.
```

## Purpose of future CONNECT-only probe

A future CONNECT-only probe would be allowed only to answer this narrow question:

```text
Can a provided, user-owned, explicitly configured MQTT auth tuple establish a broker session and return CONNACK without subscribing, publishing, or controlling a device?
```

It must not answer status/control questions. It must not discover topics by wildcard subscription. It must not send a command.

## Required evidence before implementation

Implementation remains blocked until all items below are known in non-secret masked form:

```text
1. MQTT host and port.
2. Client id format.
3. Username format.
4. Auth mode: password, token, signature, or another explicit mode.
5. Secret source: environment variable only.
6. Keep-alive value.
7. CONNECT timeout.
8. CONNACK handling behavior.
9. Immediate DISCONNECT behavior.
10. Output masking rules.
```

Missing any item keeps the gate blocked.

## Required environment contract

A future CONNECT-only stage may use only explicit environment variables:

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

Required future values:

```text
GREE_ALICE_MQTT_CLIENT_ID
GREE_ALICE_MQTT_USERNAME
GREE_ALICE_MQTT_AUTH_MODE
```

Secret values must never be printed:

```text
GREE_ALICE_MQTT_PASSWORD
GREE_ALICE_MQTT_TOKEN
```

## Fail-closed defaults

A future implementation must default to:

```text
GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true
GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=false
GREE_ALICE_MQTT_ALLOW_PUBLISH=false
```

If those values are missing or unsafe, the command must fail before opening a network connection.

## Forbidden inputs

The CONNECT-only command must not accept:

```text
topic
wildcard topic
payload
command payload
device control fields
setpoint
mode
fan speed
swing
power state
raw device key printed as an argument
raw MAC printed as an argument
raw token printed as an argument
```

## Forbidden behavior

The CONNECT-only command must not:

```text
send SUBSCRIBE
send PUBLISH
send a command topic
send a device command payload
attempt wildcard topic discovery
write raw credentials to disk
print raw credentials to console
print raw client id if configured as sensitive
print raw username if configured as sensitive
store raw CONNACK bytes if they contain sensitive data
wire into AssistantEngineer.Api
wire into Telegram bot
wire into production deployment
create migrations
change runtime config
```

## Allowed behavior

The future command may:

```text
validate inputs
mask inputs
resolve DNS
open TCP
open TLS with SNI
send MQTT CONNECT only
read one CONNACK
send DISCONNECT immediately after CONNACK or timeout
close the socket
write masked report
```

Only in a separately approved live-safety stage.

## Output contract

Allowed output:

```text
host
port
DNS resolved: yes/no
TCP connected: yes/no
TLS authenticated: yes/no
MQTT CONNECT sent: yes/no
CONNACK received: yes/no
CONNACK return code / reason code
DISCONNECT sent: yes/no
socket closed: yes/no
masked client id length bucket
masked username length bucket
auth mode
```

Forbidden output:

```text
raw password
raw token
raw signature
raw device key
raw MAC
raw private capture contents
raw full client id if it contains user/device identity
raw full username if it contains user/device identity
```

## Test requirements before live use

A future implementation must include tests for:

```text
missing required values fail closed
unsafe subscribe flag fails closed
unsafe publish flag fails closed
missing disconnect-after-connack fails closed
topic argument rejected
payload argument rejected
secret values masked
output artifact contains no raw password/token
configuration-only mode opens no network connection
```

## Current decision

```text
MQTT CONNECT implementation: blocked
MQTT SUBSCRIBE: blocked
MQTT PUBLISH: blocked
Device control: blocked
Production bridge: blocked
```

## Next stage proposal

```text
GREE-ALICE-18 — CONNECT-only input contract tests
```

That stage should add tests and validation around the existing input contract only. It should still not implement live MQTT `CONNECT`.
