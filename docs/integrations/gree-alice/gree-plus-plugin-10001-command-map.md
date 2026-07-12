# Gree Plus Plugin 10001 Command Map

## Scope

This document records the local/offline command payload shape used by GREE-ALICE-CMD-1.

GREE-ALICE-CMD-1 adds an isolated command builder in `AssistantEngineer.Tools.GreeCloudProbe`. It converts normalized internal commands into the Gree Plus payload shape:

```json
{
  "t": "cmd",
  "opt": ["..."],
  "p": ["..."]
}
```

The builder does not perform network operations. It does not send commands, does not manage transport identifiers, and does not include a device MAC field. Timers remain out of scope.

## Offline serialization

GREE-ALICE-CMD-2 adds offline compact JSON serialization for command payloads.

The serializer emits only these properties, in this exact order:

```json
{"t":"cmd","opt":["..."],"p":["..."]}
```

It validates that `opt` and `p` are present, non-empty, and have matching counts before serialization. It does not emit MAC fields, device identifiers, transport identifiers, or null fields. It does not perform live network/control operations. Timers remain out of scope.

## Offline status parsing

GREE-ALICE-STATE-1 adds an offline status parser for redacted Gree Plus device status objects observed around `getInfo` and status callback traces.

The parser accepts JSON objects only, reads known status fields such as `Pow`, `Mod`, `SetTem`, `WdSpd`, `AllErr`, `deviceState`, `status`, `mid`, and `host`, and leaves missing optional values as null. Unknown extra properties are ignored. It exposes conservative derived values such as power-on, error-present, and online-status only when the raw field is present.

The parser does not perform network reads, live control, authentication, transport work, or timer work. It is intended as a local model step for a later read-only probe and Alice query/status path.

The machine-readable payload map is stored in `gree-plus-plugin-10001-command-map.json`.

## Plugin and transport evidence

The broader API and bridge inventory is maintained in [gree-plus-api-contract-inventory.md](./gree-plus-api-contract-inventory.md). Current evidence confirms that the plugin/runtime surface includes:

```text
Network.request
Network.sendDataToDevice
Network.publishTopic
Network.setMqttStatusCallback
Network.setHomeBroadCastMsgCallback
Subscribe.subscribeDeviceStatus
Subscribe.unsubscribeDeviceStatus
Subscribe.subscribeTopics
Safety.encryptData
Safety.decryptData
Safety.jsonToHex
Safety.hexToJson
Safety.restoreHexData
```

Command helper names confirmed in plugin evidence include:

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

`sendDataToDevice` is linked to command payload assembly in plugin evidence, but this does not prove the complete live transport envelope. The current repository builder remains an offline normalized serializer only. It emits the compact `t` / `opt` / `p` body shape and does not emit device identifiers, device MAC values, authentication material, signatures, MQTT topics, `/GreeAccess/access/action` request metadata, or any live control transport wrapper.

Therefore this document does not claim that the live control contract is complete. `/GreeAccess/access/action`, MQTT publish, and any Set/Mod/Del/Clear/StartOrCancel/action-like flow remain blocked until a separate evidence and approval stage proves exact method, auth, request shape, response shape, target addressing, and safety behavior.
