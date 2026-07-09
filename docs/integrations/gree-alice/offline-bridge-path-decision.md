# GREE-ALICE-30: path decision and offline bridge skeleton boundary

## Decision

```text
Chosen path: Path B
Path B: offline Yandex Smart Home bridge skeleton first
Live MQTT CONNECT: blocked
MQTT SUBSCRIBE: blocked
MQTT PUBLISH: blocked
Device control: blocked
Gree+ runtime control: blocked
```

## Reason

The project has enough safety documentation to continue architecture work, but it does not yet have a signed approval for live MQTT operation.

Therefore the next useful release-oriented work is to prepare an isolated offline bridge skeleton and Yandex Smart Home contract boundary without sending any command to Gree+ Cloud.

## Scope of this stage

```text
Documentation-only bridge boundary.
Yandex Smart Home endpoint contract skeleton.
Fail-closed action behavior definition.
No production API changes.
No Telegram changes.
No runtime deployment changes.
No migrations.
No live Gree+ control.
```

## Next implementation path

A later stage may create a separate bounded-context project skeleton under `src/Integrations/GreeAliceBridge`, but that skeleton must remain disconnected from production runtime until explicit deployment work is opened.

```text
Next possible stage: GREE-ALICE-31 — offline bridge project skeleton
Still blocked: live CONNECT, SUBSCRIBE, PUBLISH, device control
```
