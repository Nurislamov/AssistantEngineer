# GREE-ALICE CONNECT-only dry-run command contract

## Stage

`GREE-ALICE-19` adds an offline dry-run command for the future MQTT CONNECT-only input contract.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This stage does not name or cite third-party protocol sources.

## Command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-dry-run
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-dry-run `
  --configuration-only
```

## What dry-run validates

```text
required variables are present
integer variables are within safe ranges
boolean variables are valid
SUBSCRIBE/PUBLISH flags are not true
DISCONNECT_AFTER_CONNACK is not false
topic/payload/control arguments are rejected
raw values are masked
```

## Required future values

```text
GREE_ALICE_MQTT_CLIENT_ID
GREE_ALICE_MQTT_USERNAME
GREE_ALICE_MQTT_AUTH_MODE
```

## Forbidden arguments

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

## Gates

Even when dry-run inputs are valid, all live gates remain blocked in this stage:

```text
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

A valid dry-run means only that inputs are ready for a separately approved live-safety stage.

## Safety

```text
Output contains raw values: no
Network connection opened: no
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
```

## Next stage proposal

```text
GREE-ALICE-20 — Dry-run contract tests and masked output checks
```

That future stage should add tests around the dry-run command and still avoid live MQTT CONNECT.

## GREE-ALICE-20 follow-up

`GREE-ALICE-20` adds repository guard tests for the dry-run command contract and masked-output requirements.

The tests still do not implement MQTT `CONNECT`, do not open network connections, do not subscribe, do not publish, and do not control a device.
