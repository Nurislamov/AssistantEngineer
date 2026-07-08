# GREE-ALICE masked MQTT evidence inventory

## Stage

`GREE-ALICE-15` adds an offline masked inventory for local GREE-ALICE JSON artifacts.

This stage does not name or cite third-party protocol sources. It uses only local project artifacts and writes only masked summaries.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --inventory-mqtt-evidence
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --inventory-mqtt-evidence `
  --configuration-only
```

## What the inventory records

The report may record:

```text
file path relative to repository
file size
JSON parse status
field name counts
field name classifications
string length buckets
raw leak candidate counts
MQTT signal field-name hits
sensitive/identity field-name hits
```

The report must not record raw primitive values.

## Field classifications

```text
general
sensitive-or-identity
mqtt-signal
```

`mqtt-signal` is only a field-name classification. It does not prove a real MQTT client id, username, token, topic, or payload shape.

## Raw leak candidate detection

The inventory may count potential raw leak candidates, but it must never print the raw values.

Candidate patterns include:

```text
MAC-like identifiers
email-like identifiers
private IP addresses
long token-like strings
```

## Output artifact

```text
artifacts/gree-alice/mqtt-evidence-inventory/gree-mqtt-evidence-inventory-YYYYMMDD-HHMMSS.json
```

The artifact is local diagnostic output and must not be committed.

## Safety

```text
Output contains raw values: no
Network connection opened: no
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Private capture committed: no
```

## Next stage proposal

```text
GREE-ALICE-16 — Evidence inventory interpretation and CONNECT gate decision
```

Possible scope:

```text
- interpret masked field-name inventory;
- identify whether client id/auth/topic evidence is still unknown;
- decide if CONNECT remains blocked;
- do not add network code unless a separate explicit safety stage is approved.
```
