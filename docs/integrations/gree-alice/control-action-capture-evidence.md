# GREE-ALICE control action capture evidence

## Stage

`GREE-ALICE-13` documents masked evidence from a real Gree+ control-action capture.

This stage does not name or cite third-party projects. It only records our own capture observations and internal protocol vocabulary assumptions.

This stage does not implement MQTT `CONNECT`, does not subscribe, does not publish, and does not control a device.

## Safe command

```powershell
cd D:\Project\AssistantEngineer

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --summarize-control-action-evidence `
  --control-capture-csv "$env:USERPROFILE\Downloads\PCAPdroid_08_июл._15_58_15.csv" `
  --action-sequence "off/on and setpoint 24 -> 23 -> 24"
```

Configuration-only smoke:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --summarize-control-action-evidence `
  --configuration-only
```

## Private capture evidence

The private PCAPdroid CSV captured a real Gree+ control sequence:

```text
off/on and setpoint 24 -> 23 -> 24
```

The key finding is:

```text
mqtt-hk.gree.com:1994 was active over TLS during the control-action window.
```

The same capture also showed:

```text
hkgrih.gree.com:443 HTTPS/REST traffic
UDP/7000 LAN activity
```

The capture file itself is private diagnostic material and must not be committed.

## Internal vocabulary candidates

These names are kept only as internal vocabulary candidates for future mapping:

```text
Pow
Mod
SetTem
TemUn
WdSpd
SwUpDn
SwingLfRig
Quiet
Tur
Lig
t=status
t=cmd
opt
p
```

This does not prove the Gree+ Cloud MQTT auth model, topic model, QoS, or payload envelope.

## Known unknowns

```text
Gree+ Cloud MQTT client id
Gree+ Cloud MQTT username
Gree+ Cloud MQTT password/token/signature
Gree+ Cloud MQTT status topic
Gree+ Cloud MQTT command topic
Gree+ Cloud MQTT QoS
Gree+ Cloud MQTT payload encryption/signature shape
Whether cloud payload reuses LAN-style command vocabulary directly or wraps it in another envelope
```

## Safety

```text
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Private capture committed: no
```

## Next stage proposal

```text
GREE-ALICE-14 — MQTT auth/topic evidence acquisition plan
```

That stage should define the safest practical ways to obtain MQTT client id/auth/topic evidence without sending commands.

## GREE-ALICE-14 follow-up

After the control-action capture evidence summary, the next safe step is:

```text
GREE-ALICE-14 — MQTT auth/topic evidence acquisition plan
```

This stage defines safe evidence handling for client id/auth/topic discovery. It remains documentation-only and must not implement MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or device commands.
