# GREE-ALICE-LIVE-EVIDENCE-1: read-only Gree Plus evidence capture

## Purpose

This package prepares a manual, masked evidence capture for the Gree Plus read-only auth/status contract.

It is not a live client implementation. It adds no command sending, no device control, no MQTT behavior, no production runtime wiring, no deployment changes, and no migrations.

## Capture boundary

The operator scenario is read-only:

```text
Open Gree Plus.
Open the home/device list.
Open one test air conditioner.
Wait for state/status to load.
Refresh or reopen the device screen if needed.
Do not tap power, temperature, mode, fan, swing, feature, scene, timer, or automation controls.
```

Use `adb logcat` only. Do not use mitmproxy, do not change phone proxy settings, and do not install or trust proxy certificates for this capture.

## Suggested local capture commands

Run commands outside the repository or write only to ignored local storage. Do not commit raw output.

```powershell
adb logcat -c
adb logcat -v time > <untracked-local-capture-path>
```

Stop capture after the device status is loaded. Immediately redact the capture before sharing or summarizing it.

The helper script is offline-only and has no default input path:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\redact-gree-plus-live-evidence.ps1 -InputPath <raw-capture-path> -OutputPath <redacted-output-path>
```

You may also pass a short pasted sample:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\redact-gree-plus-live-evidence.ps1 -Text "<sample>"
```

## Positive evidence to look for

Capture and summarize, if present:

```text
getSystemInfo
getUserInfo
getHomeId
getInfo
setMqttStatusCallback
apiHost
serverId
host
mid
deviceState
status
Pow
Mod
SetTem
TemUn
WdSpd
Quiet
Tur
SwUpDn
SwingLfRig
AllErr
```

Preserve endpoint paths, HTTP method names, header names, body field names, response field names, and safe status integer values when they are not tied to real identity.

## Negative evidence required

The redacted summary must explicitly state whether the capture contains any of:

```text
sendDataToDevice
cmd
action
control
publish
SetTem write
Pow write
command payload sent
```

If any write-like operation appears, do not use the capture as read-only contract evidence. Stop and document the exact redacted line and user action that caused it.

## Redaction requirements

Everything shared or committed must be redacted:

```text
email -> <EMAIL>
uid/user id -> <UID>
homeId -> <HOME_ID>
deviceId -> <DEVICE_ID>
mac -> <DEVICE_MAC>
access_token -> <ACCESS_TOKEN>
refresh_token -> <REFRESH_TOKEN>
Authorization header -> <AUTHORIZATION>
cookie/session -> <SESSION>
phone -> <PHONE>
account name -> <ACCOUNT>
local IP -> <LOCAL_IP>
```

Never commit raw logs, raw PCAPs, screenshots with identifiers, account values, MACs, tokens, cookies, home IDs, device IDs, emails, or local `.local` files.

## Review package

Create a copy of `gree-plus-live-evidence-template.md` and fill it only with masked values.

The conclusion must be one of:

```text
unknown
partial
confirmed-read-only
```

Use `confirmed-read-only` only when endpoint, method/transport, headers, body/request shape, response envelope, and negative evidence are all documented without raw identifiers or secrets.

## Next step

After the operator provides a masked evidence summary, the next possible implementation stage is:

```text
GREE-ALICE-LIVE-READ-3 Implement confirmed read-only Gree Plus status client
```

Do not start that stage unless the evidence proves the exact read-only contract and absence of command/control/action behavior.
