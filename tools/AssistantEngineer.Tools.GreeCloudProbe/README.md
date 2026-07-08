# AssistantEngineer.Tools.GreeCloudProbe

Console tool for the GREE-ALICE cloud probe.

## Purpose

This tool checks whether a Gree+ Cloud account can be used for the future Alice / Yandex Smart Home bridge.

It validates:

- selected region / server URL;
- Gree+ Cloud login;
- homes;
- rooms;
- devices;
- split AC candidates;
- VRF gateway / child-unit candidates;
- masked diagnostic output.

The tool is intentionally isolated from production runtime. It does not touch `AssistantEngineer.Api`, Telegram bot, deployment files, runtime database, or migrations.

## Safe local run

Set credentials only in the current PowerShell session:

```powershell
cd D:\Project\AssistantEngineer

$env:GREE_ALICE_GREE_USERNAME = "your_gree_plus_login"
$env:GREE_ALICE_GREE_PASSWORD = "your_gree_plus_password"
$env:GREE_ALICE_GREE_REGION = "Ouzbekistan"

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer"
```

Clear local variables after the run:

```powershell
Remove-Item Env:\GREE_ALICE_GREE_USERNAME -ErrorAction SilentlyContinue
Remove-Item Env:\GREE_ALICE_GREE_PASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:\GREE_ALICE_GREE_REGION -ErrorAction SilentlyContinue
```

## Configuration-only run

Use this mode to verify local configuration and artifact writing without cloud login:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --configuration-only
```

## Region / server override

Default region for this project is:

```text
Ouzbekistan
```

The current validated mapping for this project is East South Asia Gree+ Cloud server.

If required, override the exact server URL:

```powershell
$env:GREE_ALICE_GREE_SERVER_URL = "https://hkgrih.gree.com"
```

or:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --server-url "https://hkgrih.gree.com"
```

## Output

Default output directory:

```text
artifacts/gree-alice/probe/
```

The `artifacts/` folder is ignored by Git.

The report masks sensitive values by default.

## Supported environment variables

```text
GREE_ALICE_GREE_REGION
GREE_ALICE_GREE_SERVER_URL
GREE_ALICE_GREE_USERNAME
GREE_ALICE_GREE_PASSWORD
GREE_ALICE_OUTPUT_DIR
GREE_ALICE_TIMEOUT_SECONDS
GREE_ALICE_SAVE_RAW_RESPONSE
GREE_ALICE_MASK_SECRETS
```

## Current stage

`GREE-ALICE-03` adds real Gree+ Cloud login and device discovery to the probe tool.

The next stage should investigate the read-only MQTT/TLS channel handshake without publishing commands.

## Validated project region mapping

In the Gree+ app the Russian UI can show Uzbekistan, while the selected value can later be displayed as:

```text
Ouzbékistan
```

Validated cloud server for this account:

```text
East South Asia / https://hkgrih.gree.com
```

## Normalize latest probe report

`GREE-ALICE-03` adds a safe local normalization mode. It reads the latest masked probe report and writes a normalized device snapshot.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --normalize-latest-report
```

Optional explicit input:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --normalize-latest-report `
  --input-report ".\artifacts\gree-alice\probe\gree-cloud-probe-YYYYMMDD-HHMMSS.json"
```

Default snapshot output directory:

```text
artifacts/gree-alice/snapshots/
```

The snapshot intentionally excludes token, password, and raw device key values. It keeps only safe identifiers, masked values, classification, normalized kind, candidate control target, and raw field names for future capability mapping.

## Safe raw status and capability fields

`GREE-ALICE-03` prepares the live status/capability investigation.

Cloud discovery reports now include `SafeRawProperties` for each device. Sensitive values such as token, password, raw device key, MAC-like identifiers, email, phone, and barcode-like fields are masked.

This is useful when the physical AC is online:

```powershell
$env:GREE_ALICE_GREE_USERNAME = "your_gree_plus_login"
$env:GREE_ALICE_GREE_PASSWORD = "your_gree_plus_password"
$env:GREE_ALICE_GREE_REGION = "Ouzbekistan"

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer"

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --normalize-latest-report
```

If the physical unit is offline, the probe should still complete when Gree+ Cloud returns metadata, but live status fields may be missing or stale.

For this project, `Ouzbekistan` / `Ouzbékistan` is validated against:

```text
East South Asia / https://hkgrih.gree.com
```

## Read-only live status endpoint probe

`GREE-ALICE-03` adds a read-only live-status investigation mode.

It logs in to Gree+ Cloud, discovers homes/devices, then probes candidate `Get...` endpoints with masked reports. It does not send control commands.

```powershell
$env:GREE_ALICE_GREE_USERNAME = "your_gree_plus_login"
$env:GREE_ALICE_GREE_PASSWORD = "your_gree_plus_password"
$env:GREE_ALICE_GREE_REGION = "Ouzbekistan"

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-live-status
```

Optional limit:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-live-status `
  --max-attempts 40
```

Default output directory:

```text
artifacts/gree-alice/live-status/
```

The report stores masked endpoint attempts and highlights candidate fields such as `Pow`, `Mod`, `SetTem`, `WdSpd`, temperature, fan, swing, and status-like fields when they appear.


## Capture summary for live/control channel investigation

`GREE-ALICE-03` adds an offline summarizer for exported Gree+ app traffic observations.

It does not capture traffic, log in, or control devices. It reads a user-provided text/CSV/log export, masks sensitive values, and extracts host/path/protocol hints.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --summarize-capture `
  --capture-input "C:\path\to\gree-plus-capture-export.txt"
```

Default output:

```text
artifacts/gree-alice/channel-investigation/
```

## Current validated cloud result

A real Gree+ account test for this project confirmed:

```text
Region: Ouzbekistan
Server URL: https://hkgrih.gree.com
Login: succeeded
Homes: 1
Rooms: 1
Devices: 1
```

The first observed device was a Gree cloud AC device with a Wi-Fi module signature and masked local key. It is treated as `cloud-ac-candidate` unless future raw fields prove a VRF parent/child relationship.

## Internal device model

`GREE-ALICE-04` defines safe internal records for future bridge work:

```text
Models/GreeCloudDeviceClassifications.cs
Models/GreeCloudSafeDeviceSnapshot.cs
Models/GreeCloudDeviceStateSnapshot.cs
Models/GreeCloudObservedEndpoint.cs
```

The detailed model contract is documented in:

```text
docs/integrations/gree-alice/device-state-model.md
```

## Live/control channel investigation

`GREE-ALICE-07` records the current live/control evidence:

```text
hkgrih.gree.com:443      HTTPS REST discovery path
mqtt-hk.gree.com:1994    MQTT/TLS live channel candidate
255.255.255.255:7000     local UDP discovery fallback
```

Details:

```text
docs/integrations/gree-alice/live-control-channel-investigation.md
```

The capture CSV/PCAP itself is private diagnostic material and must not be committed.

## Read-only MQTT channel handshake probe

`GREE-ALICE-08` adds a transport-only probe for the current MQTT/TLS channel candidate:

```text
mqtt-hk.gree.com:1994
```

Safe run:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-mqtt-channel
```

This checks DNS, TCP, and TLS/SNI only. It does not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-channel-handshake.md
```

## Offline MQTT auth/topic model draft

`GREE-ALICE-09` drafts the MQTT authentication and topic model from local masked artifacts.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-model
```

This is offline only. It does not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-auth-topic-model.md
```

## Offline MQTT CONNECT-only safety review

`GREE-ALICE-10` reviews whether a future MQTT `CONNECT`-only test is justified.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --review-mqtt-connect-safety
```

This is offline only. It does not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-connect-safety-review.md
```

## Offline MQTT CONNECT input contract

`GREE-ALICE-11` defines the input contract for a future MQTT `CONNECT`-only test.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-connect-input-contract
```

This is offline only. It does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-connect-input-contract.md
```

## Offline MQTT CONNECT input validation

`GREE-ALICE-12` validates future MQTT `CONNECT` environment variables in fail-closed mode.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --validate-mqtt-connect-inputs
```

This is offline only. It does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-connect-input-validation.md
```

## Offline control action evidence summary

`GREE-ALICE-13` summarizes a private Gree+ control-action capture in masked form.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --summarize-control-action-evidence `
  --control-capture-csv "$env:USERPROFILE\Downloads\PCAPdroid_08_июл._15_58_15.csv" `
  --action-sequence "off/on and setpoint 24 -> 23 -> 24"
```

This is offline only. It does not mention third-party source names, and it does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/control-action-capture-evidence.md
```

## MQTT auth/topic evidence acquisition plan

`GREE-ALICE-14` defines safe evidence handling for future MQTT client id/auth/topic discovery.

```text
docs/integrations/gree-alice/mqtt-auth-topic-evidence-plan.md
```

This is documentation-only. It does not name third-party source projects, and it does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

## Masked MQTT evidence inventory

`GREE-ALICE-15` scans local GREE-ALICE JSON artifacts and produces a masked evidence inventory.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --inventory-mqtt-evidence
```

This is offline only. It writes field-name counts, classifications, and length buckets; it does not print raw values and does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-evidence-inventory.md
```

## MQTT evidence gate decision

`GREE-ALICE-16` interprets the latest masked evidence inventory and keeps MQTT `CONNECT` blocked unless required non-secret evidence exists.

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --decide-mqtt-evidence-gate
```

This is offline only. It does not print raw values and does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.

Details:

```text
docs/integrations/gree-alice/mqtt-evidence-gate-decision.md
```

## MQTT CONNECT-only safety specification

`GREE-ALICE-17` defines the safety contract for a possible future MQTT `CONNECT`-only probe.

```text
docs/integrations/gree-alice/mqtt-connect-only-safety-specification.md
```

This is documentation-only. It does not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.
