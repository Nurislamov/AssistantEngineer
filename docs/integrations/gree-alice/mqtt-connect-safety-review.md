# GREE-ALICE MQTT CONNECT-only safety review

## Stage

`GREE-ALICE-10` reviews whether a future MQTT `CONNECT`-only test is justified.

This stage is offline and read-only. It does not connect to MQTT, does not subscribe, does not publish, and does not control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --review-mqtt-connect-safety
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --review-mqtt-connect-safety `
  --configuration-only
```

Optional explicit model report:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --review-mqtt-connect-safety `
  --mqtt-model-report ".\artifacts\gree-alice\mqtt-model\gree-mqtt-auth-topic-model-YYYYMMDD-HHMMSS.json"
```

## Current expected decision

At the end of `GREE-ALICE-09`, the expected decision is:

```text
not-ready
```

Reason:

```text
MQTT transport is confirmed, but client id, username, password/token format, CONNECT packet options, topic model, and payload safety are still unknown.
```

## Not allowed in this stage

```text
MQTT CONNECT
MQTT SUBSCRIBE
MQTT PUBLISH
wildcard subscriptions
device control payloads
Yandex Smart Home bridge wiring
AssistantEngineer.Api changes
deployment changes
persistent credential storage
```

## Required before a future CONNECT-only implementation

```text
Confirm MQTT client id format.
Confirm MQTT username format.
Confirm MQTT password/token format.
Confirm whether auth uses cloud uid/token, device mac/key, app token, region secret, signed payload, or another value.
Define exact CONNECT-only packet options.
Define timeout and immediate disconnect behavior after CONNACK.
Define masked logging for client id, username, token, device key, and device identifiers.
Confirm that the future test sends no SUBSCRIBE, no PUBLISH, no retained payload, no will message, and no device command.
```

## Guard rails for a future CONNECT-only implementation

```text
Future test must be opt-in with a separate flag.
Future test must never run as part of normal probe flow.
Future test must not subscribe to any topic.
Future test must not publish any MQTT message.
Future test must not send power/mode/setpoint/fan/swing payloads.
Future test must not write raw credentials or raw device keys to artifacts.
Future test must mask client id, username, token, MAC-like identifiers, device keys, SSID, barcode, latitude, and longitude.
Future test must disconnect immediately after CONNECT result is known.
Future test must keep all artifacts under artifacts/gree-alice/ and out of Git.
Future test must remain isolated in tools/AssistantEngineer.Tools.GreeCloudProbe.
```

## Next stage proposal

```text
GREE-ALICE-11 — MQTT CONNECT input contract
```

This should define the exact environment variables and masking rules for a later connection-only test, without implementing the connection itself.
