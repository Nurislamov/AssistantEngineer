# GREE-ALICE MQTT evidence gate decision

## Stage

`GREE-ALICE-16` interprets the masked local inventory and decides whether MQTT `CONNECT` can move forward.

This stage does not name or cite third-party protocol sources. It uses only masked inventory produced by local project tools.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --decide-mqtt-evidence-gate
```

## Decision rule

MQTT `CONNECT` remains blocked unless the project has non-secret masked evidence for all required areas:

```text
client id format
username format
auth mode and secret source
topic handling rules
CONNECT-only behavior
CONNACK handling
immediate DISCONNECT behavior
```

Field-name signals alone are not enough.

## Current expected decision

```text
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

## Safety

```text
Output contains raw values: no
Network connection opened: no
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
```

## Next stage proposal

```text
GREE-ALICE-17 — MQTT CONNECT-only safety specification
```

That stage may prepare a specification only. It must still not implement MQTT `CONNECT` until the user explicitly approves a separate live-safety stage.
