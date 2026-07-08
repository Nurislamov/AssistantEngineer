# GREE-ALICE MQTT CONNECT readiness gate

## Stage

`GREE-ALICE-22` adds an offline readiness gate for the future MQTT CONNECT-only live-safety review.

This stage does not implement MQTT `CONNECT`, does not open DNS/TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This document does not name or cite third-party protocol sources.

## Purpose

The readiness gate reads a masked dry-run report and decides whether the project may move to a separate human live-safety review.

It does not approve live `CONNECT`.

## Command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-readiness-gate
```

Optional explicit report:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-readiness-gate `
  --dry-run-report "D:\Project\AssistantEngineer\artifacts\gree-alice\mqtt-connect-dry-run\<report>.json"
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --mqtt-connect-readiness-gate `
  --configuration-only
```

## Required dry-run evidence

A dry-run report must show:

```text
Status: dry-run-ready-for-separate-live-safety-stage
CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
Output contains raw values: no
Network connection opened: no
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
```

## Readiness interpretation

If all checks pass, the readiness gate may report:

```text
Status: ready-for-human-live-safety-review
Live CONNECT gate: blocked-pending-explicit-human-approval
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

This means only that a separate human review may be considered. It is not permission to run live MQTT `CONNECT`.

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

## Blocked cases

The readiness gate must block when:

```text
dry-run report is missing
dry-run report is not ready
dry-run report is not masked
dry-run report says network was opened
dry-run report says MQTT CONNECT was sent
dry-run report says SUBSCRIBE/PUBLISH/control was sent
dry-run gates are no longer blocked
forbidden topic/payload/control argument is provided
```

## Next stage proposal

```text
GREE-ALICE-23 — CONNECT-only human safety review checklist
```

That future stage should remain documentation-only unless explicitly approved otherwise.
