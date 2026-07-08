# GREE-ALICE CONNECT-only dry-run operator guide

## Stage

`GREE-ALICE-21` documents safe operator usage for the offline MQTT CONNECT-only dry-run command.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This guide does not name or cite third-party protocol sources.

## Goal

The dry-run command exists to validate a future CONNECT-only input contract without touching the broker or the device.

It answers only this question:

```text
Are the planned CONNECT-only inputs structurally valid, masked, and safe enough for a separate live-safety review?
```

It does not prove that credentials work. It does not prove that a device is controllable. It does not discover topics.

## Command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-dry-run
```

## Safe defaults

The following values should be used for any local dry-run that is intended to pass structural validation:

```powershell
$env:GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK = "true"
$env:GREE_ALICE_MQTT_ALLOW_SUBSCRIBE = "false"
$env:GREE_ALICE_MQTT_ALLOW_PUBLISH = "false"
```

These defaults still do not approve a live CONNECT. They only keep the offline dry-run aligned with the safety contract.

## Required values

The dry-run requires these structural values for a future live-safety stage:

```powershell
$env:GREE_ALICE_MQTT_CLIENT_ID = "<masked-or-dummy-client-id>"
$env:GREE_ALICE_MQTT_USERNAME = "<masked-or-dummy-username>"
$env:GREE_ALICE_MQTT_AUTH_MODE = "token"
```

Allowed auth modes:

```text
password
token
signature
```

## Optional values

```powershell
$env:GREE_ALICE_MQTT_HOST = "mqtt-hk.gree.com"
$env:GREE_ALICE_MQTT_PORT = "1994"
$env:GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS = "60"
$env:GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS = "5"
```

## Secret handling

Do not paste real secrets into chat.

When testing locally, set secrets only in the current PowerShell process:

```powershell
$env:GREE_ALICE_MQTT_TOKEN = "<local-secret-not-for-commit>"
```

The dry-run report must show only masked values and length buckets.

## Forbidden arguments

Never pass these to dry-run:

```text
--topic
--payload
--command
--cmd
--power
--pow
--setpoint
--set-tem
--settem
--mode
--fan
--swing
```

If any of these are present, the dry-run must fail closed.

## Expected successful structural dry-run

A structurally valid dry-run may show:

```text
Status: dry-run-ready-for-separate-live-safety-stage
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
Network connection opened: no
MQTT CONNECT sent: no
```

The important part is that gates remain blocked. A valid dry-run is not permission to run a live CONNECT.

## Expected blocked dry-run

Missing required inputs should show:

```text
Status: blocked-fail-closed
Missing required inputs: 3
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

Unsafe flags should also block:

```text
GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true
GREE_ALICE_MQTT_ALLOW_PUBLISH=true
GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=false
```

## Repository safety

Do not commit:

```text
artifacts/gree-alice/
private PCAP or CSV captures
real tokens
real passwords
real device keys
raw MAC values
raw account identifiers
```

## Next step

A future stage may add more guard tests around sample matrix output. Live MQTT CONNECT still requires a separate explicit safety approval.

## GREE-ALICE-22 follow-up

`GREE-ALICE-22` adds an offline readiness gate that reads a masked dry-run report and decides whether a separate human live-safety review may be considered.

The readiness gate still does not implement MQTT `CONNECT`, open network connections, subscribe, publish, or control a device.
