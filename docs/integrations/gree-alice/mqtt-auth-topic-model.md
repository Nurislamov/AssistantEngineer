# GREE-ALICE MQTT authentication and topic model

## Stage

`GREE-ALICE-09` drafts the MQTT authentication and topic model from local masked artifacts.

This stage is offline and read-only. It does not connect to MQTT, does not subscribe, does not publish, and does not control a device.

## Inputs

The model draft uses local artifacts if they exist:

```text
artifacts/gree-alice/probe/gree-cloud-probe-*.json
artifacts/gree-alice/mqtt-channel/gree-mqtt-channel-probe-*.json
```

These artifacts are ignored by Git and must not be committed.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-model
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-model `
  --configuration-only
```

Optional explicit inputs:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-model `
  --input-report ".\artifacts\gree-alice\probe\gree-cloud-probe-YYYYMMDD-HHMMSS.json" `
  --mqtt-report ".\artifacts\gree-alice\mqtt-channel\gree-mqtt-channel-probe-YYYYMMDD-HHMMSS.json"
```

## Current known facts

```text
REST discovery server: https://hkgrih.gree.com
MQTT channel candidate: mqtt-hk.gree.com:1994
MQTT transport: DNS/TCP/TLS/SNI confirmed
Cloud discovery provides device metadata and key presence
Raw keys, raw tokens, raw MAC values, SSID, barcode, and geolocation must stay out of committed files
```

## Unknowns

```text
MQTT client id format
MQTT username format
MQTT password/token format
topic naming
payload encryption/signature shape
whether CONNECT can be safely tested without subscribing
whether status is pushed only after subscription
```

## Not allowed yet

```text
MQTT CONNECT
MQTT SUBSCRIBE
MQTT PUBLISH
wildcard subscription
power/mode/setpoint/fan/swing command payloads
Yandex Smart Home bridge wiring
production API/deploy changes
persistent credential storage
```

## Next stage proposal

```text
GREE-ALICE-10 — MQTT CONNECT-only safety review
```

Only start it after reviewing the GREE-ALICE-09 model draft and deciding what credentials, client id, and safety constraints are required for a connection-only test.

## Useful artifact selection

The model draft prefers useful local artifacts instead of the newest file by timestamp.

For cloud discovery it prefers reports with discovered devices or successful cloud login.

For the MQTT channel probe it prefers reports with successful TLS or TCP transport.

This avoids accidentally selecting a newer `configuration-only` smoke report that contains no devices.

## GREE-ALICE-10 follow-up

After the offline auth/topic model draft, the next safe step is:

```text
GREE-ALICE-10 — MQTT CONNECT-only safety review
```

This review remains offline and must not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.
