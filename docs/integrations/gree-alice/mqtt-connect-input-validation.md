# GREE-ALICE MQTT CONNECT input validation scaffold

## Stage

`GREE-ALICE-12` implements offline validation of future MQTT `CONNECT` environment variables.

This stage does not implement MQTT `CONNECT`. It does not open TCP/TLS/MQTT connections, subscribe, publish, or control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --validate-mqtt-connect-inputs
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --validate-mqtt-connect-inputs `
  --configuration-only
```

## Expected result with no secrets configured

```text
Validation status: blocked-fail-closed
Missing required inputs: 3
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
```

Missing required inputs:

```text
GREE_ALICE_MQTT_CLIENT_ID
GREE_ALICE_MQTT_USERNAME
GREE_ALICE_MQTT_AUTH_MODE
```

This is the safe default.

## Validation rules

```text
Fail closed when client id is missing.
Fail closed when username is missing.
Fail closed when auth mode is missing or not one of password/token/signed.
Fail closed when password auth mode has no password.
Fail closed when token auth mode has no token.
Fail closed when signed auth mode is selected because signed auth is not implemented yet.
Reject GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true.
Reject GREE_ALICE_MQTT_ALLOW_PUBLISH=true.
Require GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true.
Reject topic inputs and command payload inputs.
```

## Unsafe inputs rejected

```text
GREE_ALICE_MQTT_TOPIC
GREE_ALICE_MQTT_SUBSCRIBE_TOPIC
GREE_ALICE_MQTT_PUBLISH_TOPIC
GREE_ALICE_MQTT_WILDCARD_TOPIC
GREE_ALICE_MQTT_COMMAND_PAYLOAD
GREE_ALICE_MQTT_POWER
GREE_ALICE_MQTT_MODE
GREE_ALICE_MQTT_SETPOINT
GREE_ALICE_MQTT_FAN
GREE_ALICE_MQTT_SWING
```

## Masking

The validator must not print raw secrets. For secret values it reports only:

```text
provided / missing
length bucket
```

Raw values that must not appear in artifacts:

```text
client id
username
password
token
device key
MAC-like identifiers
SSID
barcode
latitude
longitude
topic payloads
control payloads
```

## Output artifact

```text
artifacts/gree-alice/mqtt-connect-input-validation/gree-mqtt-connect-input-validation-YYYYMMDD-HHMMSS.json
```

The artifact is local diagnostic output and must not be committed.

## Next stage proposal

```text
GREE-ALICE-13 — MQTT CONNECT evidence plan
```

That stage should define how to obtain or infer the real client id/auth fields safely before any live MQTT `CONNECT` implementation.

## GREE-ALICE-13 follow-up

After the input validation scaffold, the next safe step is:

```text
GREE-ALICE-13 — Control action capture evidence
```

This stage documents the new control-action capture in masked form. It must not mention third-party source names and must not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.
