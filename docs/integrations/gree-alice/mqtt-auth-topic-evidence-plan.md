# GREE-ALICE MQTT auth/topic evidence acquisition plan

## Stage

`GREE-ALICE-14` defines safe ways to obtain MQTT client id, auth, and topic evidence.

This stage is documentation-only. It does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This document must not name or cite third-party protocol sources. It is based only on our own masked capture summaries, local artifacts, and user-provided non-secret observations.

## Current evidence

Validated facts already recorded by previous stages:

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

Current missing evidence:

```text
MQTT client id
MQTT username
MQTT password/token/signature
MQTT status topic
MQTT command topic
QoS
Payload encryption/signature shape
Whether cloud payload reuses LAN-style command vocabulary directly or wraps it in another envelope
```

## Allowed evidence sources

Only masked and non-secret evidence is allowed.

```text
1. GreeCloudProbe masked artifacts under artifacts/gree-alice/.
2. PCAPdroid flow summaries: host, port, protocol, timing, byte/packet counts.
3. Manual user-provided non-secret observations from the Gree+ app UI.
4. Local string inventory from user-owned app files if done later and if it does not include raw credentials or private tokens.
5. Local generated summaries that mask identifiers before writing output.
```

Allowed output examples:

```text
MQTT host: mqtt-hk.gree.com
MQTT port: 1994
Control action timing: 2026-07-08T15:57:32+05:00 -> 2026-07-08T15:58:07+05:00
Client id pattern candidate: <masked-pattern-only>
Topic pattern candidate: <masked-pattern-only>
Token presence: yes/no
Token length bucket: 17-32 / 33-64 / 65+
```

## Forbidden evidence handling

The following must not be committed, printed, or stored in docs:

```text
Raw Gree+ password
Raw account token
Raw MQTT password
Raw MQTT token
Raw device key
Raw MAC address
Raw device id if it can identify the user/device
Raw private IP when not necessary
Raw PCAP/CSV capture files
Command payloads that could control a device
Wildcard subscribe topics
```

## Safe acquisition sequence

### Step 1 — capture-only confirmation

Repeat a control-action capture if needed and summarize only these fields:

```text
host
port
protocol
first seen
last seen
bytes sent
bytes received
packets sent
packets received
action sequence description
```

Do not store the capture file in the repository.

### Step 2 — discovery artifact inventory

Review existing masked GreeCloudProbe artifacts and record only:

```text
region
server URL
home count
room count
device count
device classification
presence of key/token fields
length buckets
field-name inventory without raw values
```

### Step 3 — app-string inventory plan

If still blocked, prepare a separate explicit stage for local string inventory.

That future stage may search for non-secret strings such as:

```text
mqtt
clientId
username
topic
subscribe
publish
token
password
device
home
room
```

It must not bypass security protections, must not intercept decrypted traffic, and must not store raw secrets.

### Step 4 — CONNECT-only decision gate

MQTT `CONNECT` remains blocked unless all conditions below are met:

```text
Client id format is known or safely derived.
Username format is known or safely derived.
Auth mode is known: password, token, or signed.
Secret value source is known and can be passed only through environment variables.
Output masking is already implemented and tested.
CONNECT-only behavior is specified: send CONNECT, read CONNACK, immediately DISCONNECT.
SUBSCRIBE and PUBLISH flags remain false.
No command topic or command payload is configured.
```

If any condition is missing, the decision remains:

```text
CONNECT status: blocked
```

## Future CONNECT-only guard rails

A future explicit safety stage must enforce:

```text
GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=false
GREE_ALICE_MQTT_ALLOW_PUBLISH=false
GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true
No topic input
No command payload input
No device-control fields
No wildcard topics
Masked output only
Local artifacts only
```

## Decision after GREE-ALICE-14

```text
MQTT CONNECT implementation: still blocked
MQTT SUBSCRIBE: blocked
MQTT PUBLISH: blocked
Device control: blocked
Production bridge: blocked
```

The next safe step is an explicit evidence-only stage, not a connection stage, unless client id/auth/topic evidence becomes known in a non-secret masked form.

## Next stage proposal

```text
GREE-ALICE-15 — MQTT evidence inventory from local masked artifacts
```

Possible scope:

```text
- scan artifacts/gree-alice/ for safe JSON reports;
- produce a masked inventory of field names and length buckets;
- do not read raw credentials into logs;
- do not open network connections;
- do not implement CONNECT/SUBSCRIBE/PUBLISH/control.
```
