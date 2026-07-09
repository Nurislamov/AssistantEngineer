# Local Yandex Provider Smoke Expectations

## Scenario Matrix

| Scenario | Input | Expected output | Safety expectation |
| --- | --- | --- | --- |
| linked user /devices | `masked-yandex-user-001` | scoped dummy split AC and exposed VRF child units | no global registry leakage |
| linked user /query | known split AC / VRF child IDs | offline fixture state | no live Gree+ Cloud or MQTT |
| linked user /action | known split AC / VRF child IDs | dry-run fail-closed | no command execution |
| linked user /unlink | dummy linked user | offline/template unlink result | no real token or secret deletion claim |
| unknown user /devices | unknown masked user | empty/fail-closed scope | no dummy devices by default |
| unknown device /query | unknown dummy device ID | controlled offline unknown | no live lookup |
| unknown device /action | unknown dummy device ID | controlled fail-closed result | no command execution |
| VRF child exposure | dummy VRF child units | exposed Yandex user devices | stable IDs and room-friendly names |
| gateway not exposed | dummy VRF gateway | absent from Yandex devices | gateway remains internal |
| provider readiness not-ready | readiness evaluator | not-ready / not-approved | production remains blocked |

## Input

All inputs use dummy/template or masked values only.

## Expected Output

The harness returns an `offline-pass` result only when every scenario passes.

Each scenario returns step-level pass/fail data.

## Safety Expectation

The harness must not call real Yandex, implement OAuth, use real credentials/tokens, call live Gree+ Cloud, use MQTT, control devices, deploy anything, or execute commands.

## Pass/Fail Interpretation

`offline-pass` means the local offline contract smoke passed.

It does not mean the provider is production-ready.

## Known Blocked Behavior

Real OAuth, production provider registration, production endpoint configuration, live Gree+ Cloud integration, MQTT, and device control remain blocked.

The public `/devices` endpoint still needs a future user-context enforcement stage.

## Future Production Smoke Delta

Future production smoke would add provider registration evidence, OAuth evidence, production endpoint checks, external callback checks, credential storage evidence outside the repository, monitoring evidence, rollback evidence, and operator approval.
