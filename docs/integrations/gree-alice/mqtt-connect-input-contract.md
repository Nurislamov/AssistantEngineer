# GREE-ALICE MQTT CONNECT input contract

## Stage

`GREE-ALICE-11` defines the input contract for a future MQTT `CONNECT`-only test.

This stage does not implement MQTT `CONNECT`. It does not subscribe, publish, or control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-connect-input-contract
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --draft-mqtt-connect-input-contract `
  --configuration-only
```

## Future environment variables

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

These names are reserved for a future connection-only implementation. Do not put raw values into the repository.

## Current status

```text
Contract status: draft-connect-still-blocked
MQTT CONNECT implementation: not included
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
```

## Required future validation

```text
Fail closed when client id is missing.
Fail closed when username is missing.
Fail closed when auth mode is unknown.
Fail closed when both password and token are supplied unless a signed mode explicitly allows it.
Fail closed when neither password nor token nor signed credentials are supplied.
Reject GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true.
Reject GREE_ALICE_MQTT_ALLOW_PUBLISH=true.
Require GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true for the first live test.
Reject wildcard topic inputs.
Reject power/mode/setpoint/fan/swing command inputs.
```

## Masking rules

```text
Never write raw Gree+ password to artifacts.
Never write raw MQTT password/token to artifacts.
Never write raw device key to artifacts.
Never write raw MAC-like identifiers to committed files.
Never write SSID, barcode, latitude, or longitude to committed files.
Mask client id and username unless proven non-sensitive.
For secrets, report only provided/not-provided and length bucket.
For identifiers, report only stable masked form.
Keep all reports under artifacts/gree-alice/ and out of Git.
```

## Output artifact shape

```text
artifacts/gree-alice/mqtt-connect-input-contract/gree-mqtt-connect-input-contract-YYYYMMDD-HHMMSS.json
```

Required top-level sections:

```text
Stage
Mode
TimestampUtc
Summary
InputContract
ValidationRules
MaskingRules
ArtifactShape
Blockers
Safety
Notes
```

## Next stage proposal

```text
GREE-ALICE-12 — MQTT CONNECT input validation scaffold
```

That stage may implement validation of local environment variable presence and masking, but still must not open a MQTT connection.
