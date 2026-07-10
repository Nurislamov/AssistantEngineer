# GREE-ALICE Internal Offline Release Notes Draft

## Release name

Release name: GREE-ALICE-RC1

## Status

Status: RC1 / INTERNAL OFFLINE RELEASE CANDIDATE / NOT PRODUCTION

## Scope

Scope: internal/offline only.

Base commit: b60fb382
RC commit: to be created by this stage
Validation: pending before commit, then PASS after checks
Production Yandex release: NOT READY

## Included capabilities

- isolated GreeAliceBridge API;
- offline Yandex Smart Home endpoints;
- dummy split AC device;
- VRF/GMV child-unit model;
- gateway hidden by default;
- offline registry/import boundary;
- offline account linking boundary;
- provider readiness package;
- local smoke harness;
- PowerShell smoke script;
- localhost-only HTTP smoke boundary;
- dry-run fail-closed /action.

## Excluded capabilities

- real Yandex provider registration;
- real OAuth;
- real credentials/tokens;
- production endpoint;
- production deployment;
- live Gree+ Cloud read-only;
- live Gree+ Cloud control;
- MQTT;
- device control;
- admin UI;
- multi-account rollout.

## Validation summary

Validation must include full solution restore/build/test, `git diff --check`, local smoke script, and local HTTP smoke when a local isolated bridge API is available.

## Known limitations

- not usable by real Yandex app as production provider;
- no real account linking;
- no real Gree+ runtime data;
- no real control;
- offline fixture data only;
- manual smoke/evidence still required before any pilot.

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
