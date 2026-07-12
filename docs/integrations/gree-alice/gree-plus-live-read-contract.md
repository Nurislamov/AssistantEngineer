# GREE-ALICE-LIVE-READ-2: Gree Plus read-only contract scaffold

## Purpose

This document captures the current read-only Gree Plus live access evidence and the contract gaps that still block a real live read.

This stage remains read-only. It adds no command execution, no device control, no MQTT behavior, no production runtime wiring, no deployment changes, and no migrations.

## Current conclusion

```text
Read-only cloud contract confirmed: no
Implementation status: evidence-backed scaffold
Default live read result: NotReady
Network attempted by default: false
Command/control behavior: absent
Alice production readiness: NOT READY
```

The available repository evidence is enough to model the open questions, but not enough to implement a real authenticated Gree Plus status request without guessing. The probe therefore remains fail-closed.

## Known evidence

The full evidence-backed candidate inventory is now maintained in [gree-plus-api-contract-inventory.md](./gree-plus-api-contract-inventory.md). This document keeps only the live read contract implications and gaps.

Observed app and region evidence:

```text
Package: com.gree.greeplus
App version observed: 1.25.3.7
Plugin: 10001
apiHost/debugServer: https://hkgrih.gree.com
serverId: 2
language: ru_RU
timeZoneOffset: 300
Observed domains: hkgrih.gree.com, eugrih.gree.com, globalacsinfo.gree.com, cloudinfo-abd.gree.com, hk.dis.gree.com
```

Observed app/API strings:

```text
/App/GetHomes
/App/GetHomeMsg
/App/GetMsg
/App/QueryOnline runtime-observed online/state poll candidate, about every 8 seconds
/App/OptHistory runtime-observed history/sync candidate
/GreeAccess/access/action statically discovered and runtime-correlated command endpoint candidate
/Stats/GetHomeDevEnergyData
/Stats/GetHomeDevEnergySummary
access_token
homeId
deviceId
login
verifyToken
isSupportMqtt
gr_bd_access_token_key
gr_bd_region_index
```

Observed bridge/status functions:

```text
jsBridge.getSystemInfo
jsBridge.getUserInfo
jsBridge.getHomeId
getInfo
setMqttStatusCallback
sendDataToDevice
```

Expanded bridge/plugin surface confirmed by the static Flutter/plugin evidence:

```text
Network.request
Network.downloadFile
Network.downloadPlugin
Network.getServerTime
Network.sendDataToDevice
Network.publishTopic
Network.setMqttStatusCallback
Network.setHomeBroadCastMsgCallback
UserData.getUserInfo
UserData.getHomeId
UserData.getHomeList
UserData.getRoomList
UserData.getDeviceList
UserData.getDeviceStatus
UserData.getDeviceConnect
UserData.getHomeInfo
UserData.getDeviceInfo
UserData.getDevicesData
UserData.getAppConfig
Subscribe.subscribeDeviceStatus
Subscribe.unsubscribeDeviceStatus
Subscribe.subscribeTopics
Safety.encryptData
Safety.decryptData
Safety.jsonToHex
Safety.hexToJson
Safety.restoreHexData
```

Aggregate discovery checkpoint:

```text
Static APK inventory: PASS
Flutter/plugin contract extraction: PASS
PluginArtifactCount: 68
PluginJadxSuccessCount: 27
LibAppStringCount: 26950
LibAppFocusedCount: 1921
LibAppEndpointCount: 32
PluginFocusedHitCount: 2529
PluginContractHitCount: 2525
PluginEndpointCount: 235
CombinedContractCount: 4699
AccessActionStaticHits: 1
SendDataToDevicePluginHits: 40
MqttPluginHits: 7
```

Observed status fields are already covered by `GreePlusDeviceStatusParser`, including `Pow`, `Mod`, `SetTem`, `WdSpd`, `AllErr`, `deviceState`, `status`, `mid`, and `host`.

## Confirmed parts

The following are confirmed enough for offline modeling only:

```text
Region and server hints exist.
Homes/message/statistics path candidates exist.
Session and device identifier names are visible in app strings.
Plugin 10001 status fields can be parsed from an operator-provided JSON object.
The existing parser is deterministic and offline-only.
```

## Unknown parts

The following are not confirmed and block a real live read:

```text
region selection: exact region-to-host resolution contract
authentication/session: login, token refresh, request signing, and required headers
homes discovery: exact request method, body/query shape, and response envelope
device discovery: exact device list shape and selected-device binding
status read: exact read-only endpoint, request method, headers, and response envelope
status read: exact request body/query shape and signing/encryption requirements
status read: proof of non-mutation for the selected path
read-only proof: evidence that the selected status path cannot mutate device/account state
command/control: /GreeAccess/access/action contract remains high risk and must not be blind-probed
MQTT: authentication, topic, callback/publish/subscribe, and payload envelope remain unresolved
```

Because these are unknown, the live probe must return `NotReady` with `NetworkAttempted=false`.

## Why MITM is not used

MITM interception failed because the Gree Plus app did not trust the proxy certificate. This stage does not add certificate bypasses, app patching, proxy trust changes, or any other certificate trick.

Future evidence must come from operator-approved local artifacts, official documentation, static analysis that does not contain secrets, or a separately approved live read-only procedure.

## Safety gate before any live read

A future live read attempt still requires all of:

```text
GREE_ALICE_ENABLE_LIVE_READ=true
explicit --approve-read-only operator approval
one allowlisted device alias
external operator file or environment-only config
confirmed read-only endpoint/auth/status contract
redacted diagnostics and result values
tests proving default behavior attempts no network
```

Credentials and identifiers must stay outside the repository. No `.local` file, token, access token, refresh token, password, email, uid, homeId, deviceId, mac, credential, or real account/device value may be committed.

## Explicitly forbidden

This stage does not add and must not call:

```text
sendDataToDevice execution
/GreeAccess/access/action execution
SetTem command sending
Pow command sending
mode/fan/swing/feature writes
MQTT CONNECT
MQTT SUBSCRIBE
MQTT PUBLISH
timers or schedules
production API/Telegram runtime wiring
deployment changes
migrations
```

## Implemented scaffold behavior

The code now exposes a contract report with:

```text
Status: EvidencePartial
KnownEvidence: high-level observed evidence only
Gaps: region selection, session, homes discovery, device discovery, status read
IsReadOnlyContractConfirmed: false
```

`GreePlusLiveReadProbe` uses this report when `ExactReadContractKnown` is false and returns a `ContractUnknown` result reason. It still does not attempt the network.

## Next evidence required

Before the first real read-only status check, capture masked evidence for:

```text
exact read-only endpoint path
request method
required headers with values redacted
request body/query shape with placeholders only
status response envelope
mapping from response payload to plugin 10001 status JSON
single operator-approved alias
proof that no command/control/action path is called
rollback step that disables GREE_ALICE_ENABLE_LIVE_READ or removes external credentials
```

The next implementation step is to replace only the `status read` gap with a reviewed, fake-transport-tested read-only transport abstraction after the contract is confirmed. Do not proceed to live control until a separate `GREE-ALICE-LIVE-CONTROL-GATE-1` approval exists.
