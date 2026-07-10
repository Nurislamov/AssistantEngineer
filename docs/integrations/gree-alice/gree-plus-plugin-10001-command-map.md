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

The machine-readable payload map is stored in `gree-plus-plugin-10001-command-map.json`.
