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

The redaction helper script is offline-only and has no default input path:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\redact-gree-plus-live-evidence.ps1 -InputPath <raw-capture-path> -OutputPath <redacted-output-path>
```

You may also pass a short pasted sample:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\redact-gree-plus-live-evidence.ps1 -Text "<sample>"
```

After redaction, run the extractor only on the redacted or super-redacted file:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\extract-gree-plus-live-evidence.ps1 -InputPath <redacted-output-path> -OutputDirectory <untracked-extract-directory>
```

The extractor writes small local files such as `status-evidence.txt`, `control-risk-evidence.txt`, `negative-control-proof.txt`, `contract-gaps.txt`, `leak-check.txt`, and `summary.md`. Keep generated evidence files outside Git unless a later stage explicitly approves a masked summary format.

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

`sendDataToDevice` may appear in analytics, click traces, or bridge logging. Treat that as a risk candidate, not proof of live control by itself. Confirmed control proof requires stronger markers such as `t=cmd`, `control_order`, `dev_control`, MQTT publish, or a write/control/action request carrying a command body.

Status fields such as `Pow`, `Mod`, `SetTem`, and `WdSpd` inside `t=status` or `fullstatueJson` are status evidence, not command proof.

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
task/window token -> <WINDOW_TOKEN>
analytics appliance name -> <DEVICE_ALIAS_OR_MAC>
```

Never commit raw logs, raw PCAPs, screenshots with identifiers, account values, MACs, tokens, cookies, home IDs, device IDs, emails, or local `.local` files.

Current evidence status: manual read-only capture can confirm plugin/callback/status shape, including `cordova.callbackFromNative`, `fullstatueJson`, `t=status`, and safe status fields. It still does not confirm the exact HTTP live read endpoint, method, headers, request body, and response envelope needed for a live status client.

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
