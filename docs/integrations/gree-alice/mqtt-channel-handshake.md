# GREE-ALICE MQTT channel handshake probe

## Stage

`GREE-ALICE-08` adds a read-only MQTT channel handshake probe.

This stage is transport-only. It does not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or any control command.

## Target

The current channel candidate from GREE+ traffic is:

```text
Host: mqtt-hk.gree.com
Port: 1994
Expected layer: TCP + TLS/SNI
```

## What the probe checks

The probe checks:

```text
DNS resolution
TCP connection
TLS/SNI authentication
remote certificate summary
```

The probe intentionally does not check:

```text
MQTT username/password
MQTT client id
MQTT topics
MQTT subscribe
MQTT publish
device command payloads
```

## Safe local run

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-mqtt-channel
```

Optional explicit target:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-mqtt-channel `
  --mqtt-host "mqtt-hk.gree.com" `
  --mqtt-port 1994 `
  --timeout-seconds 20
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --probe-mqtt-channel `
  --configuration-only
```

## Output

Default local output:

```text
artifacts/gree-alice/mqtt-channel/
```

The output folder is ignored by Git.

## Expected interpretation

If DNS, TCP, and TLS succeed, the next stage can investigate MQTT authentication and topic discovery.

If DNS/TCP succeeds but TLS fails, keep the result and inspect whether the app uses another TLS profile or whether the capture only showed an IP/port association.

If TCP fails, check network, firewall, ISP, DNS, or regional reachability before moving to MQTT protocol work.

## Safety boundaries

Allowed:

```text
- DNS lookup
- TCP connect
- TLS handshake with SNI
- certificate metadata summary
```

Not allowed in this stage:

```text
- no MQTT CONNECT
- no MQTT SUBSCRIBE
- no MQTT PUBLISH
- no device control
- no Yandex bridge
- no production API/deploy changes
- no credential storage
```

## Next stage proposal

```text
GREE-ALICE-09 вЂ” MQTT authentication/topic model discovery
```

Only start it after `GREE-ALICE-08` proves whether `mqtt-hk.gree.com:1994` is reachable from the intended environment.
