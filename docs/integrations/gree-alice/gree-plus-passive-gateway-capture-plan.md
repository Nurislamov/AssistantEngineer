# Gree Plus Passive Wi-Fi Gateway Capture Plan

## Purpose

Define a safe passive metadata capture plan for GREE-ALICE-GATEWAY-CAPTURE-1. The goal is channel and timing correlation, not endpoint scanning, HTTPS decryption, command replay, or device control.

## Architecture

```text
Internet/upstream
-> Windows 11 laptop
-> hotspot/gateway
-> Samsung test phone
-> GREE+
```

## What passive gateway capture can reveal

```text
DNS
destination IP
ports
TCP/UDP flows
TLS handshake
SNI/server certificate where visible
timing
packet sizes
connection reuse
correlation between user action and network flow
HTTPS/MQTT/UDP channel separation
```

## What it cannot reveal without TLS session secrets or approved MITM

```text
URL path inside HTTPS
HTTP headers
Authorization
JSON request/response body
tokens
opt/p command body
homeId/deviceId values
```

## Required hardware/network shape

Preferred:

```text
upstream Ethernet, USB modem, or second Wi-Fi adapter
separate Wi-Fi adapter/hotspot for phone
Windows routing/ICS or another controlled gateway method
```

Do not assume one Wi-Fi adapter can always work stably as both upstream client and hotspot at the same time.

## Capture scenarios

| Scenario | Action | Safety status | Expected correlation |
| --- | --- | --- | --- |
| CAP-00 | Baseline, app closed | Allowed | Background phone/network noise baseline. |
| CAP-01 | Launch GREE+ | Allowed | DNS/TLS/session startup flows. |
| CAP-02 | Open device list | Allowed | Discovery/list refresh flows. |
| CAP-03 | Open one device | Allowed | Device detail/status channel selection. |
| CAP-04 | Manual refresh/no command | Allowed | Read-only refresh timing. |
| CAP-05 | One temperature change in app | Blocked unless separately approved | Control/write flow correlation only after explicit control approval. |
| CAP-06 | One power toggle | Blocked unless separately approved | Control/write flow correlation only after explicit control approval. |
| CAP-07 | Physical remote change | Allowed | State feedback without app command. |
| CAP-08 | App background/foreground | Allowed | Reconnect/session reuse behavior. |
| CAP-09 | Wait for periodic QueryOnline poll | Allowed | Periodic HTTPS poll timing, about every 8 seconds if reproduced. |
| CAP-10 | Observe reconnect after Wi-Fi interruption | Blocked unless separately approved | Reconnect behavior can disturb device/app state. |

For the current safe documentation/capture preparation stage, CAP-06 is blocked unless an explicit separate control approval exists.

## Capture procedure

1. Prepare a controlled Windows gateway/hotspot and connect only the Samsung test phone when possible.
2. Start Wireshark/Npcap capture on the gateway-facing interface before launching GREE+.
3. Record wall-clock timestamps for each scenario start/stop.
4. Keep each scenario short and separated by a quiet interval.
5. Stop immediately if unexpected control, account prompt, firmware update, or destructive flow appears.
6. Save PCAP only to local untracked evidence storage.
7. Export only a sanitized summary into repository docs when a later stage approves it.

## Time synchronization

Synchronize Windows time before capture. Record local timezone, capture start time, phone visible time, and scenario timestamps. Use relative offsets in public summaries.

## Wireshark/Npcap filters

Capture filter candidates:

```text
host <phone-ip>
tcp or udp
port 443 or port 1994 or port 7000 or port 53
```

Display filter candidates:

```text
ip.addr == <phone-ip>
tls || tcp || udp || dns
tcp.port == 443 || tcp.port == 1994 || udp.port == 7000 || dns
```

Replace `<phone-ip>` only in local notes. Do not commit real local IP addresses if they identify the capture environment.

## Evidence naming

Use local names like:

```text
GREE-ALICE-GATEWAY-CAPTURE-1_CAP-01_launch_<local-timestamp>.pcapng
GREE-ALICE-GATEWAY-CAPTURE-1_correlation-worksheet.local.csv
GREE-ALICE-GATEWAY-CAPTURE-1_sanitized-summary.md
```

PCAP and local worksheets stay outside Git.

## Redaction

Redact or omit account identifiers, emails, local IPs, device identifiers, device MAC values, SSID, tokens, cookies, authorization values, exact home/device IDs, and raw payloads. Public summaries may include only domains, ports, protocol family, relative timing, packet counts, byte counts, and masked scenario labels.

## PCAP handling

PCAP files remain local and must never be committed. Do not attach raw PCAP to commits, issues, PRs, or documentation. Do not convert PCAP into CSV and commit it.

## Correlation worksheet

| Scenario | Local start offset | Local stop offset | User action | DNS names | Destination ports | Flow notes | Redaction status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CAP-00 | TBD | TBD | App closed | TBD | TBD | TBD | Local only |
| CAP-01 | TBD | TBD | Launch app | TBD | TBD | TBD | Local only |
| CAP-02 | TBD | TBD | Open device list | TBD | TBD | TBD | Local only |
| CAP-03 | TBD | TBD | Open one device | TBD | TBD | TBD | Local only |
| CAP-04 | TBD | TBD | Manual refresh/no command | TBD | TBD | TBD | Local only |
| CAP-05 | TBD | TBD | Temperature change | TBD | TBD | Blocked unless approved | Local only |
| CAP-06 | TBD | TBD | Power toggle | TBD | TBD | Blocked unless approved | Local only |
| CAP-07 | TBD | TBD | Physical remote change | TBD | TBD | TBD | Local only |
| CAP-08 | TBD | TBD | Background/foreground | TBD | TBD | TBD | Local only |
| CAP-09 | TBD | TBD | Wait for periodic poll | TBD | TBD | TBD | Local only |
| CAP-10 | TBD | TBD | Wi-Fi interruption reconnect | TBD | TBD | Blocked unless approved | Local only |

## Success criteria

```text
Phone traffic is isolated enough to correlate flows with scenarios.
DNS and destination host/port candidates are identified.
HTTPS/MQTT/UDP channel separation is visible.
Periodic QueryOnline-like timing is confirmed or not reproduced without guessing.
No raw identifiers or secrets enter repository files.
No endpoint is probed blindly.
No control scenario runs without separate explicit approval.
```

## Stop conditions

```text
Unexpected command/control UI action occurs.
App requests account re-login or exposes identifiers on screen/logs.
Phone joins the wrong network.
Capture includes unrelated personal traffic that cannot be isolated.
Firmware update, pairing, reset, or destructive flow appears.
Any token/credential/raw account value would need to be written into a repository file.
```

## Follow-up

Laptop gateway capture is useful as passive metadata capture. It is not automatic HTTPS decryption. The next step after metadata correlation is focused JSBridge argument capture or an approved TLS-key acquisition path, not blind endpoint scanning.
