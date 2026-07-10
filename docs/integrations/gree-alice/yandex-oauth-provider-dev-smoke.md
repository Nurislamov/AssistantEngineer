# Yandex OAuth Provider Dev Smoke

## Purpose

Document the PILOT-1B dev-only/local OAuth provider smoke path.

## Boundary

- Dev-only/local only.
- Uses in-memory authorization code and token records.
- Uses dummy/offline devices only.
- Does not perform production deploy.
- Does not call Yandex live endpoints.
- Does not call Gree+ live endpoints.
- Does not use real OAuth secrets or real tokens.
- Does not use MQTT.
- Does not control devices.
- `/action` remains dry-run fail-closed.
- Production Yandex release remains NOT READY.

## Local flow

```text
GET /oauth/authorize
  -> dev-only authorization code
POST /oauth/token
  -> Bearer token JSON
GET /v1.0/user/devices
  -> dummy/offline devices
POST /v1.0/user/devices/query
  -> offline fixture state
POST /v1.0/user/devices/action
  -> dry-run fail-closed
POST /v1.0/user/unlink
  -> offline unlink response and dev-only token revocation
```

## Smoke command

Run the API locally on an explicit localhost port, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 `
  -RepoRoot . `
  -SkipRestore `
  -SkipBuild `
  -RunOAuthSmoke `
  -LocalBaseUrl "http://localhost:5005"
```

The script must not print bearer values. Evidence should record PASS/FAIL only, request IDs, endpoint categories, and masked/dummy identifiers.
