# GREE-ALICE Internal Offline Release Notes Draft

## Release name

GREE-ALICE Internal Offline RC

## Status

Status: DRAFT / NOT RELEASED

## Scope

Internal/offline engineering release candidate for the isolated `GreeAliceBridge` bounded context.

## Included capabilities

- Offline bridge contracts.
- Offline Yandex Smart Home API skeleton.
- Offline registry and registry import/admin boundary.
- VRF/GMV child-unit dummy exposure model.
- Account linking boundary.
- Provider readiness package.
- Local smoke harness.
- Operator runbook.
- PowerShell smoke script.
- Localhost-only HTTP smoke boundary.
- Safety guards and tests.

## Excluded capabilities

- Real Yandex provider registration.
- Real OAuth implementation.
- Real Yandex client credentials.
- Access/refresh tokens.
- Production endpoint.
- Production deployment.
- Live Gree+ Cloud read-only integration.
- Live Gree+ Cloud control.
- MQTT.
- Device control.
- Admin UI.
- Multi-account rollout.

## Validation summary

Validation must include full solution restore/build/test, `git diff --check`, local smoke script, and local HTTP smoke when a local isolated bridge API is available.

## Known limitations

This release candidate is offline/local only. It uses dummy/template data and does not prove real provider registration, real OAuth, production hosting, live Gree+ behavior, MQTT behavior, or device control.

## Safety boundaries

- No real Yandex calls.
- No real OAuth credentials.
- No real tokens.
- No live Gree+ Cloud.
- No MQTT.
- No device control.
- No production deployment.
- `/action` remains dry-run fail-closed.
- Provider readiness remains NOT READY.
- Production pilot remains NOT APPROVED.

## How to run local smoke

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 -RepoRoot . -SkipRestore -SkipBuild
```

Optional localhost HTTP smoke when the isolated bridge API is running:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 `
  -RepoRoot . `
  -SkipRestore `
  -SkipBuild `
  -RunHttpSmoke `
  -LocalBaseUrl "http://localhost:<local-port>"
```

## Next steps after RC

1. Archive masked RC evidence.
2. Decide whether to tag the internal/offline RC.
3. Choose a separate production pilot path only after explicit approval.
