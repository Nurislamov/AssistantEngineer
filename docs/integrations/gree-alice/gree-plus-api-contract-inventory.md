# Gree Plus API Contract Inventory

## Purpose

This document consolidates the current evidence-backed candidate inventory for the Gree Plus application API surface relevant to GREE-ALICE.

It is a documentation-only checkpoint. It does not add live network calls, MQTT behavior, device control, runtime configuration, migrations, deployment changes, or production wiring.

## Safety and non-claims

This is not a complete official API specification. It is an evidence-backed candidate inventory assembled from prior safe observation, static APK inventory, Flutter/Dart `libapp.so` strings, private plugin artifacts, and existing GREE-ALICE documentation.

Unknown method, authentication, request shape, response shape, signing, encryption, and mutation behavior remain unknown unless explicitly marked as observed. Mutating, destructive, and command-like endpoints must never be blind-probed.

## Evidence checkpoint

```text
Package: com.gree.greeplus
Observed app version: 1.25.3.7
Static APK inventory run: run-20260712-103917
Static APK result: GREE_APK_STATIC_INVENTORY_PASS
Flutter/plugin run: run-20260712-111123
Flutter/plugin result: GREE_FLUTTER_PLUGIN_CONTRACT_PASS
libapp.so SHA-256: 43071232B93304D2F2249DE1CB2EC72D9BDD5F42451EBF1D17CDCA9E70E7A720
Leak check: PASS
```

Aggregate evidence:

```text
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

## Evidence confidence model

```text
High: static evidence plus runtime correlation, or a specific plugin callsite.
Medium: static path, bridge, or plugin evidence without proven runtime request contract.
Low: string candidate without a confirmed callsite.
```

## Runtime-observed endpoints

| Path | Observation | Host/channel | Method | Auth/session | Request shape | Response shape | Safety | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| /App/QueryOnline | Runtime-observed periodic online/state poll, about every 8 seconds | hkgrih.gree.com, HTTPS 443 | Unknown | Unknown | Unknown | Unknown | RuntimeObservedNeedsCapture | Not found as a simple static path candidate in this checkpoint; exact contract still incomplete. |
| /App/OptHistory | Runtime-observed history/sync candidate | hkgrih.gree.com, HTTPS 443 | Unknown | Unknown | Unknown | Unknown | RuntimeObservedNeedsCapture | Exact method, headers, body, and response contract remain unconfirmed. |
| /GreeAccess/access/action | Statically discovered in `libapp.so`; runtime-correlated command endpoint candidate | HTTPS candidate; exact channel unresolved | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Mutating/high risk command candidate; never blind-probe. |

No MQTT constructor events were observed during the prior safe 90-second observer window. That does not prove MQTT is absent. The plugin surface and earlier capture evidence still contain MQTT candidates, callbacks, and publish/subscribe surfaces.

## Canonical application endpoint inventory

The static application inventory below contains 86 unique clean Gree application path candidates: 77 `/App/*`, 8 `/Stats/*`, and 1 `/GreeAccess/*`. It excludes Android XML schema URLs, framework/library URLs, help/static asset URLs, and duplicate paths.

| Path | Family | Likely operation | Mutation class | Evidence | Runtime observed | Method | Auth/session | Request shape | Response shape | Safe probe class | Confidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| /App/AckHomeInvite | App | Acknowledge home invite | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Invitation/account membership candidate. |
| /App/AddHomeMember | App | Add home member | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Membership write candidate. |
| /App/AddOrMoveDevToRoom | App | Add or move device to room | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Home topology write candidate. |
| /App/ChangeHomeOwner | App | Change home owner | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Account ownership write candidate. |
| /App/CreateNewRoom | App | Create room | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Room write candidate. |
| /App/DelCloudTimerData | App | Delete cloud timer | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Timer deletion candidate. |
| /App/DelCloudTimerDataV2 | App | Delete cloud timer V2 | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Timer deletion candidate. |
| /App/DeleteRoom | App | Delete room | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Room deletion candidate. |
| /App/DelHome | App | Delete home | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Home deletion candidate. |
| /App/DelInteractiveSceneData | App | Delete interactive scene | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Scene deletion candidate. |
| /App/DelSceneData | App | Delete scene | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Scene deletion candidate. |
| /App/EmailVerify | App | Email verification | Unknown | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | UnknownNeedsReview | Medium | Auth/account lifecycle candidate. |
| /App/ExportUserData | App | Export user data | Unknown | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | UnknownNeedsReview | Medium | Privacy/account export candidate. |
| /App/Fault/GetFaultInformation | App | Get fault information | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Fault read candidate. |
| /App/GetCardCloudedInfo | App | Get card cloud info | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | UI/cloud info read candidate. |
| /App/GetCloudTimerByMac | App | Get cloud timer by device | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Device identifier value must remain external/redacted. |
| /App/GetCloudTimerByMacV2 | App | Get cloud timer by device V2 | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Device identifier value must remain external/redacted. |
| /App/GetCloudTimerPushStatus | App | Get cloud timer push status | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Timer notification status candidate. |
| /App/GetDevCustomData | App | Get device custom data | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Device data read candidate. |
| /App/GetDevsInRoomsOfHomeV1 | App | Get devices in rooms of home V1 | ReadCandidate | Plugin static endpoint inventory | Existing discovery evidence for V2 only | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Homes/devices discovery family. |
| /App/GetDevsInRoomsOfHomeV2 | App | Get devices in rooms of home V2 | ReadCandidate | Existing docs and plugin static endpoint inventory | Yes, prior discovery | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | High | Metadata discovery confirmed; live status/control not confirmed. |
| /App/GetDevsInRoomsOfUserHomesV1 | App | Get devices in rooms of user homes V1 | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Homes/devices discovery family. |
| /App/GetDevsNotInRoomOfHomeV1 | App | Get devices not in room of home V1 | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Home topology read candidate. |
| /App/GetDevsOfUserHomes | App | Get devices of user homes | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Homes/devices discovery family. |
| /App/GetElecTarget | App | Get electricity target | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Energy target read candidate. |
| /App/GetHomeInfo | App | Get home info | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Home metadata read candidate. |
| /App/GetHomeMsg | App | Get home messages | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Message read candidate. |
| /App/GetHomes | App | Get homes | ReadCandidate | Existing docs, static APK, plugin/libapp inventory | Yes, prior discovery | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | High | Homes discovery path confirmed at high level; exact public contract still incomplete. |
| /App/GetInteractiveSceneInfo | App | Get interactive scene info | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetInteractiveSceneInfoV2 | App | Get interactive scene info V2 | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetInteractiveSceneOfHome | App | Get interactive scenes of home | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetInteractiveSceneOfUserHomes | App | Get interactive scenes of user homes | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetMembersInUserHomes | App | Get members in user homes | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Membership read candidate. |
| /App/GetMsg | App | Get messages | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Message read candidate. |
| /App/GetProvincesOrCountiesList | App | Get provinces/counties list | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Region/address metadata read candidate. |
| /App/GetRecommendedMenu | App | Get recommended menu | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | UI/menu read candidate. |
| /App/GetRoomsInfoOfHome | App | Get room info of home | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Home topology read candidate. |
| /App/GetSceneInfo | App | Get scene info | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetSceneInfoBySid | App | Get scene info by scene id | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetSceneInfoBySidV2 | App | Get scene info by scene id V2 | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetScenes | App | Get scenes | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetScenesInRoomsOfHome | App | Get scenes in rooms of home | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene/topology read candidate. |
| /App/GetScenesOfUserHomes | App | Get scenes of user homes | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene read candidate. |
| /App/GetUserData | App | Get user data | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Account data read candidate. |
| /App/GetUserDev | App | Get user devices | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Device discovery candidate. |
| /App/IsFamilyMember | App | Check family membership | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Membership check candidate. |
| /App/IsInteractiveSceneNameDuplicated | App | Check interactive scene name duplicate | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene validation candidate. |
| /App/IsSceneNameDuplicated | App | Check scene name duplicate | ReadCandidate | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene validation candidate. |
| /App/IsSensitiveWord | App | Check sensitive word | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Text validation candidate. |
| /App/ModCloudTimerData | App | Modify cloud timer | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer write candidate. |
| /App/ModCloudTimerDataV2 | App | Modify cloud timer V2 | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer write candidate. |
| /App/ModHomeAddress | App | Modify home address | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Account/home write candidate. |
| /App/ModHomeName | App | Modify home name | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Home write candidate. |
| /App/ModHomeNoteName | App | Modify home note name | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Home write candidate. |
| /App/ModifyRoom | App | Modify room | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Room write candidate. |
| /App/ModInteractiveSceneData | App | Modify interactive scene | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/ModInteractiveSceneDataV2 | App | Modify interactive scene V2 | Mutating | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/NewHome | App | Create new home | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Home write candidate. |
| /App/OpenOrCloseCloudTimerPush | App | Open/close cloud timer push | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer notification write candidate. |
| /App/QuitHome | App | Leave home | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Membership write candidate. |
| /App/RemoveUserFromHome | App | Remove user from home | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Membership removal candidate. |
| /App/SendHomeInvite | App | Send home invite | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Invitation write candidate. |
| /App/SendVerifyEmail | App | Send verification email | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Account lifecycle write candidate. |
| /App/SetCloudTimerData | App | Set cloud timer | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer write candidate. |
| /App/SetCloudTimerDataV2 | App | Set cloud timer V2 | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer write candidate. |
| /App/SetDevCustomData | App | Set device custom data | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Device data write candidate. |
| /App/SetElecTarget | App | Set electricity target | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Energy target write candidate. |
| /App/SetInteractiveSceneData | App | Set interactive scene | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/SetInteractiveSceneDataV2 | App | Set interactive scene V2 | Mutating | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/SetSceneData | App | Set scene | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/SetSceneDataV2 | App | Set scene V2 | Mutating | Static APK and libapp.so endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene write candidate. |
| /App/SetUserData | App | Set user data | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Account data write candidate. |
| /App/SortRoom | App | Sort rooms | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Home topology write candidate. |
| /App/StartOrCancelCloudTimerV2 | App | Start/cancel cloud timer V2 | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Timer write candidate. |
| /App/StartOrCancelScene | App | Start/cancel scene | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene execution candidate. |
| /App/Time | App | Server time | Unknown | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | UnknownNeedsReview | Medium | Time utility candidate; exact semantics unknown. |
| /App/TurnOnOrOffInteractiveSceneRules | App | Turn scene rules on/off | Mutating | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | Medium | Scene rule write candidate. |
| /GreeAccess/access/action | GreeAccess | Device command/action candidate | Command | libapp.so static endpoint plus runtime correlation | Correlated | Unknown | Unknown | Unknown | Unknown | DoNotBlindProbe | High | High-risk command endpoint candidate; never blind-probe. |
| /Stats/ClearCloudTimerLog | Stats | Clear cloud timer log | Destructive | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Log deletion candidate. |
| /Stats/ClearHomeDevEnergyData | Stats | Clear home device energy data | Destructive | libapp.so static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Energy data deletion candidate. |
| /Stats/ClearSceneLog | Stats | Clear scene log | Destructive | libapp.so and plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | DestructiveDoNotProbe | Medium | Log deletion candidate. |
| /Stats/GetCloudTimerLog | Stats | Get cloud timer log | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Timer log read candidate. |
| /Stats/GetHomeDevEnergyData | Stats | Get home device energy data | ReadCandidate | Existing docs and libapp.so static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Energy read candidate. |
| /Stats/GetHomeDevEnergySummary | Stats | Get home device energy summary | ReadCandidate | Existing docs and libapp.so static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Energy read candidate. |
| /Stats/GetSceneLog | Stats | Get scene log | ReadCandidate | Plugin static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene log read candidate. |
| /Stats/GetSceneLogV2 | Stats | Get scene log V2 | ReadCandidate | libapp.so static endpoint inventory | No | Unknown | Unknown | Unknown | Unknown | ReadOnlyCandidateNeedsContract | Medium | Scene log read candidate. |

## Endpoint family summary

```text
Static /App/* candidates: 77
Static /Stats/* candidates: 8
Static /GreeAccess/* candidates: 1
Static clean application path candidates total: 86

Runtime-observed /App/* endpoints outside the static path inventory: /App/QueryOnline, /App/OptHistory
Runtime-correlated command endpoint candidate: /GreeAccess/access/action
```

## Plugin and bridge surface

Confirmed bridge/plugin method evidence:

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

## Command payload evidence

Confirmed command/payload helper names in plugin evidence:

```text
getPowCmd
getPowOnCmd
getPowOffCmd
getTempCmd
toSendJson
toSendStatesJson
toSendTempJson
toSendTurnOffJson
praseEntityToJson
praseJsonToEntity
```

The existing offline normalized builder emits only:

```json
{"t":"cmd","opt":["..."],"p":["..."]}
```

That offline serializer does not emit real device identifiers, device MAC values, authentication material, signatures, live transport envelopes, MQTT topics, or `/GreeAccess/access/action` request bodies. It is not proof of a complete live transport contract.

## Region/host/channel evidence

Existing safe GREE-ALICE docs and prior captures include:

```text
REST/cloud discovery host observed: hkgrih.gree.com
HTTPS port observed: 443
MQTT/TLS channel candidate from prior capture: mqtt-hk.gree.com:1994
Local UDP discovery fallback observed in prior private capture: 255.255.255.255:7000
```

GREE+ uses multiple channel candidates. HTTPS REST is confirmed for discovery. `QueryOnline` polling was runtime-observed. `/GreeAccess/access/action` is statically discovered and runtime-correlated as a command endpoint candidate. MQTT remains a candidate, not the only assumed path.

## What is confirmed

```text
Static APK inventory PASS.
Flutter/plugin extraction PASS.
Application path inventory has 86 clean static candidates.
QueryOnline and OptHistory were runtime-observed as HTTPS candidates.
/GreeAccess/access/action is statically discovered and runtime-correlated as a high-risk command candidate.
Plugin bridge contains request, sendDataToDevice, MQTT callback, publish, subscribe, UserData, and Safety surfaces.
Java APK shell alone does not contain the complete cloud request builder.
Significant logic lives in Flutter/Dart AOT libapp.so and runtime plugins.
JADX APK analysis alone is insufficient.
PluginJsonKeyCount=0 does not mean JSON is absent.
Minified JavaScript object literals and dynamic payload assembly require focused extraction or runtime capture.
```

## What remains unknown

```text
Exact region-to-host resolution contract.
Exact authentication/session lifecycle.
Required headers and signing/encryption.
HTTP methods for candidate paths.
Request body/query shapes.
Response envelopes and error model.
Proof that any selected read candidate is non-mutating.
Exact command endpoint method/body/auth/response contract.
MQTT authentication, topics, payload envelopes, and state/control split.
VRF gateway and child-unit addressing contract.
```

## Probe safety classification

```text
ReadOnlyCandidateNeedsContract: static read-like candidate, no live request until contract is proven.
RuntimeObservedNeedsCapture: runtime-observed path, needs passive/focused capture before implementation.
DoNotBlindProbe: mutating or command-like candidate.
DestructiveDoNotProbe: destructive/delete/clear/remove candidate.
UnknownNeedsReview: semantics are not clear enough for any live request.
```

## Raw evidence handling policy

Do not commit raw evidence artifacts: APK, JAR, DEX, SO, PCAP, CSV, full raw logs, tokens, passwords, device keys, device MAC values, account identifiers, SSID, email, UID, home identifiers, or device identifiers.

Repository docs may record only the evidence type, run identifier, app version, binary SHA-256, aggregate safe counts, sanitized path candidates, and redacted conclusions.

## Next evidence step

```text
GREE-ALICE-GATEWAY-CAPTURE-1 -- passive Wi-Fi gateway metadata capture and channel correlation.
```

The next step should correlate DNS, destination IP, ports, timing, TLS/SNI where visible, and user actions. It must not become blind endpoint scanning. After metadata correlation, the next deeper evidence path is focused JSBridge argument capture or an approved TLS-key acquisition path.
