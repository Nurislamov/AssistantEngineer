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
