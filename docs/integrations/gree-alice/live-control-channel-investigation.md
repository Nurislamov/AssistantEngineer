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

The likely live/control path is not the simple HTTPS REST `Get...` family.

The next technical target is the GREE+ cloud MQTT/TLS path:

```text
mqtt-hk.gree.com:1994
```

At this point we should still treat it as a read-only investigation target until authentication, topics, payload shape, and safety boundaries are understood.

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
GREE-ALICE-08 — Read-only MQTT channel handshake investigation
```

Initial scope:

```text
- define MQTT candidate configuration for hkgrih / mqtt-hk;
- attempt only safe TCP/TLS/MQTT connection diagnostics if credentials are understood;
- do not subscribe/publish to unknown topics until topic/auth model is known;
- keep reports masked and local under artifacts/.
```
