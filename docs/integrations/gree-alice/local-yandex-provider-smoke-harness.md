# Local Yandex Provider Smoke Harness

## Purpose

This stage adds a local offline smoke harness for the Yandex Smart Home provider flow.

The harness checks the contract shape as if Yandex called the bridge, but it runs entirely inside the application layer with offline/template data.

## Current Status

Harness is offline-local only.

Harness does not call real Yandex.

Harness does not implement OAuth.

Harness does not use real credentials/tokens.

Harness does not call live Gree+ Cloud.

Harness does not use MQTT.

Harness does not control devices.

Harness does not deploy anything.

Provider readiness remains NOT READY.

Production pilot remains NOT APPROVED.

## Out Of Scope

This stage does not add real Yandex calls, real OAuth, production provider registration, production endpoints, production deploy, live Gree+ Cloud integration, MQTT, device control, CLI scripts, admin endpoints, runtime configuration, or artifacts.

## What The Local Harness Checks

The harness checks:

```text
dummy linked Yandex user
scoped registry
/devices offline flow
/query offline flow
/action dry-run fail-closed flow
/unlink offline flow
unknown user fail-closed scope
unknown device query/action fail-closed behavior
VRF child unit exposure
gateway not exposed
account-linking template
provider readiness not-ready
```

## Smoke Scenarios

```text
linked-user-devices
linked-user-query
linked-user-action-fail-closed
linked-user-unlink
unknown-user-devices-fail-closed
unknown-device-query-fail-closed
unknown-device-action-fail-closed
vrf-child-unit-exposure
gateway-not-exposed
account-linking-template
registry-scope-template
provider-readiness-not-ready
```

## Expected Results

The default result is `offline-pass` when every offline expectation passes.

Actions must remain `dry-run-fail-closed` and must not send anything to Gree Cloud, MQTT, or devices.

Unknown users must not receive the global registry.

Unknown devices must return controlled offline unknown or fail-closed behavior.

## Known Limitations

The current public `/devices` skeleton does not yet enforce Yandex user context. The harness checks user-scope behavior through the scoped registry resolver and documents endpoint user-context enforcement as a future stage.

## How This Differs From Production Smoke

Production smoke would involve real provider registration, real OAuth, production endpoints, production credential storage outside the repository, and live provider callbacks.

This harness does none of those things.

## Safety Boundaries

```text
No real Yandex calls
No real OAuth
No real credentials/tokens
No live Gree+ Cloud calls
No MQTT
No production endpoint
No production deployment
No device control
No actual command execution
```

## Future Operator Smoke Script

GREE-ALICE-51 may add a local bridge runbook and operator smoke script boundary. That stage should still avoid live services and production wiring unless separately approved.

## Next Stage

GREE-ALICE-51 should add local bridge runbook and operator smoke script boundary.
