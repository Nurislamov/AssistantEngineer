# GREE-ALICE live/control channel investigation

## Stage

`GREE-ALICE-07` records the current evidence for the actual Gree+ live/control channel.

This stage is read-only. It does not send control commands, does not publish MQTT messages, does not change devices, and does not wire anything into production API, Telegram, deployment, runtime database, or migrations.

## Current confirmed baseline

Cloud discovery is confirmed through the HTTPS REST path:

```text
Region: Ouzbekistan / Ouzbékistan
REST server: https://hkgrih.gree.com
Login endpoint: /App/UserLoginV2
Homes endpoint: /App/GetHomes
Devices endpoint: /App/GetDevsInRoomsOfHomeV2
Homes: 1
Rooms: 1
Devices: 1
```

The first observed device remains a cloud-visible room climate candidate. It has a masked cloud key and a Wi-Fi module signature, but it must not be treated as proven VRF control until parent/child or gateway fields are confirmed.

## GREE-ALICE-06 REST status result

`GREE-ALICE-06` tried read-only candidate `Get...` endpoints for live state.

Result:

```text
Candidate REST endpoints attempted: 32
Result: all attempted /App/Get... live-status endpoints returned HTTP 404
Live capability fields found: none
Missing fields: Pow / Mod / SetTem / WdSpd / temperature / fan / swing
```

Conclusion:

```text
GetDevsInRoomsOfHomeV2 provides metadata only.
Simple REST /App/GetDeviceStatus-style endpoints are not the live status/control channel on hkgrih.gree.com.
```

## Capture evidence

A private GREE+ mobile app traffic export showed these relevant paths:

```text
hkgrih.gree.com:443      HTTPS REST / cloud discovery path
mqtt-hk.gree.com:1994    MQTT/TLS live channel candidate
255.255.255.255:7000     local UDP discovery fallback
```

The CSV / PCAP export itself is private diagnostic material and must not be committed.

## Working conclusion

Historical GREE-ALICE-06/07/08 results remain valid checkpoints, but they are superseded and expanded by the later static APK and Flutter/plugin evidence in [gree-plus-api-contract-inventory.md](./gree-plus-api-contract-inventory.md).

The likely live/control path is not the simple HTTPS REST `Get...` family. GREE+ uses multiple channel candidates:

```text
HTTPS REST discovery is confirmed.
/App/QueryOnline was runtime-observed as a periodic online/state poll candidate.
/App/OptHistory was runtime-observed as a history/sync candidate.
/GreeAccess/access/action is statically discovered and runtime-correlated as a high-risk command endpoint candidate.
The plugin bridge contains sendDataToDevice, request, MQTT callbacks, publish, and subscribe surfaces.
MQTT/TLS remains a channel candidate, including mqtt-hk.gree.com:1994 from prior private capture evidence.
```

The exact transport selection, auth/session lifecycle, method/body/header contract, command envelope, MQTT topic/auth model, and response shapes remain unresolved. MQTT is still a candidate, not the only assumed path.

The next evidence target is passive gateway capture for channel/timing correlation, followed by focused call-argument correlation. It must not become blind endpoint scanning.

## Safety boundaries

Allowed next actions:

```text
- document channel candidates;
- parse sanitized capture summaries;
- identify MQTT host/port/client metadata;
- add read-only MQTT connection scaffolding only if no publish/control happens;
- mask all credentials, tokens, keys, MAC-like values, SSID, barcode, latitude/longitude.
```

Not allowed in this stage:

```text
- no MQTT publish;
- no command endpoint calls;
- no power/mode/setpoint changes;
- no production bridge;
- no Yandex Smart Home endpoint;
- no persistent credential storage;
- no committing capture CSV/PCAP artifacts.
```

## Next stage proposal

```text
GREE-ALICE-GATEWAY-CAPTURE-1 — passive Wi-Fi gateway metadata capture and channel correlation
```

Initial scope:

```text
- capture passive metadata only;
- correlate user action timing with DNS, destination IP, ports, TLS/SNI where visible, packet sizes, and connection reuse;
- keep PCAP and raw local worksheets outside Git;
- do not call Set/Mod/Del/Clear/StartOrCancel/action endpoints;
- do not run MQTT CONNECT, SUBSCRIBE, or PUBLISH without a separate approved stage.
```

## GREE-ALICE-08 implementation note

Historical GREE-ALICE-08 proposed a transport-only MQTT channel probe for:

```text
mqtt-hk.gree.com:1994
```

The probe checks only DNS, TCP, and TLS/SNI. It does not send MQTT `CONNECT`, `SUBSCRIBE`, `PUBLISH`, or any device command.

That historical boundary remains safe, but the current discovery conclusion is broader: passive gateway capture should first establish which channel is actually used for launch, discovery, device view, refresh, polling, and approved future control scenarios.
