# GREE-ALICE-07 Live/control channel investigation

## Current evidence

GREE-ALICE-03 confirmed that the Gree+ Cloud account can log in and discover one device through the East South Asia server:

```text
Server: https://hkgrih.gree.com
Homes: 1
Rooms: 1
Devices: 1
Device: AC3167
Version: V3.4.M
Device key: provided
```

GREE-ALICE-05 added safe raw cloud metadata snapshots.

GREE-ALICE-06 added a read-only live-status probe and tested simple `/App/Get...` endpoint guesses. All 32 attempted live-status endpoint candidates returned HTTP 404.

## Conclusion

`GetDevsInRoomsOfHomeV2` is a metadata discovery endpoint. It does not expose live status fields such as:

```text
Pow
Mod
SetTem
WdSpd
SwUpDn
SwLfRig
TemSen
```

The actual Gree+ live/control channel is still unknown. It may be another HTTPS endpoint, WebSocket, MQTT, or another app-specific IoT channel.

## Safety rules

```text
- Do not store Gree+ credentials in files or Git.
- Do not commit artifacts under artifacts/gree-alice/.
- Do not run control commands from AssistantEngineer yet.
- Do not share raw captures containing tokens/passwords.
- Use only accounts/devices that we own or are authorized to analyze.
- Prefer host/path/protocol-level observations first.
```

## Capture summary workflow

GREE-ALICE-07 adds a local summarizer for exported capture text:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --summarize-capture `
  --capture-input "C:\path\to\gree-plus-capture-export.txt"
```

The command does not capture traffic. It only reads a user-provided export, masks sensitive values, and writes a summarized report to:

```text
artifacts/gree-alice/channel-investigation/
```

The report extracts:

```text
- hosts
- URL path shapes
- ports
- protocol hints
- keyword hits
- sanitized preview
```

## Recommended manual capture process

Use a phone or emulator where the Gree+ app is installed.

Start with a non-decrypting capture if possible:

```text
1. Start capture.
2. Open Gree+.
3. Open AC3167 device screen.
4. Wait until live status is visible in the app.
5. Change nothing first; stop capture.
6. Export text/CSV/HTTP summary if the capture app supports it.
7. Run --summarize-capture.
```

Only after read-only status traffic is understood, do a separate controlled capture for one action:

```text
1. Start capture.
2. Open AC3167.
3. Change target temperature by 1 degree once.
4. Stop capture immediately.
5. Export and summarize.
```

The second capture is for protocol discovery only. AssistantEngineer must not send commands until the protocol and safety boundaries are understood.

## What to look for

High-value observations:

```text
- any host not already known as hkgrih.gree.com
- wss:// or websocket hints
- mqtt or ports 1883/8883
- paths outside /App/GetHomes and /App/GetDevsInRoomsOfHomeV2
- payload keys similar to Pow, Mod, SetTem, WdSpd, TemSen
- device id / mac / hid routing fields after masking
```

## Next implementation step

After a sanitized capture summary identifies the actual host/path/protocol, add a targeted read-only probe for that channel.
