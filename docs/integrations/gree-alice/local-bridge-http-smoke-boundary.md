# Local Bridge HTTP Smoke Boundary

## Purpose

Define how to smoke-test the isolated `GreeAliceBridge` HTTP endpoints through localhost only.

HTTP smoke is localhost-only.
It does not call real Yandex.
It does not implement real OAuth.
It does not use real credentials/tokens.
It does not call live Gree+ Cloud.
It does not use MQTT.
It does not control devices.
It does not deploy production.
Provider readiness remains NOT READY.
Production pilot remains NOT APPROVED.

## Current status

The HTTP smoke boundary is a local/offline operator check. It is not a production pilot and does not change provider readiness.

## Scope

Allowed targets:

```text
http://localhost:<local-port>
http://127.0.0.1:<local-port>
```

The smoke checks cover `/health`, `/v1.0/user/devices`, `/v1.0/user/devices/query`, `/v1.0/user/devices/action`, and `/v1.0/user/unlink`.

## Out of scope

Real Yandex endpoints, real OAuth endpoints, real Gree+ Cloud endpoints, MQTT endpoints, production endpoints, public VPS endpoints, device control, and actual command execution are out of scope.

## Localhost-only rule

Use `http` only. The host must be `localhost` or `127.0.0.1`. The port must be explicit. Do not use `https`, public hostnames, public IP addresses, production domains, Yandex/Gree domains, OAuth endpoints, or MQTT endpoints.

## How to run isolated bridge locally

Run only the isolated bridge API project:

```powershell
dotnet run --project .\src\Integrations\GreeAliceBridge\AssistantEngineer.GreeAliceBridge.Api\AssistantEngineer.GreeAliceBridge.Api.csproj
```

Use the local port printed by the developer run output. Replace `<local-port>` with that local port.

## How to run HTTP smoke script

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 `
  -RepoRoot . `
  -SkipRestore `
  -SkipBuild `
  -RunHttpSmoke `
  -LocalBaseUrl "http://localhost:<local-port>"
```

The script rejects non-local URLs.

## Endpoint checklist

- `GET /health`
- `GET /v1.0/user/devices`
- `POST /v1.0/user/devices/query`
- `POST /v1.0/user/devices/action`
- `POST /v1.0/user/unlink`

## Expected /health result

Expected: local success response with offline runtime/safety state. No external services are contacted.

## Expected /devices result

Expected: offline dummy/template response containing `dummy-gree-ac-001` and exposed VRF child units. The VRF gateway must not appear as a user-facing device.

## Expected /query result

Safe dummy payload:

```json
{
  "devices": [
    { "id": "dummy-gree-ac-001" },
    { "id": "yandex-dummy-vrf-child-living-001" },
    { "id": "unknown-device-001" }
  ]
}
```

Expected: known devices return offline fixture state, unknown devices return a controlled offline unknown/fail-closed result, no live Gree+ Cloud call, and no MQTT.

## Expected /action fail-closed result

Safe dummy payload for the existing isolated API model:

```json
{
  "devices": [
    {
      "id": "dummy-gree-ac-001",
      "capabilities": []
    },
    {
      "id": "yandex-dummy-vrf-child-living-001",
      "capabilities": []
    },
    {
      "id": "unknown-device-001",
      "capabilities": []
    }
  ]
}
```

Expected: action returns dry-run/fail-closed; `SentToGreeCloud = false`; `SentToMqtt = false`; `SentToDevice = false`; no command execution.

## Expected /unlink result

Expected: offline/template unlink result, no real token deletion, no real secret deletion, and registry scope access revoked only in result/model.

## Unknown device behavior

Unknown dummy device IDs must return controlled offline unknown or fail-closed results. They must not trigger live discovery or command execution.

## VRF child-unit exposure behavior

Exposed VRF child units must appear as local dummy Yandex user devices with stable IDs.

## Gateway hidden behavior

The VRF/GMV gateway remains hidden/internal by default and must not be returned as a user-facing Yandex device.

## Pass criteria

- Local base URL is `http://localhost:<local-port>` or `http://127.0.0.1:<local-port>`.
- All five HTTP checks return expected local/offline results.
- `/action` remains dry-run fail-closed and sends nothing to Gree+ Cloud, MQTT, or devices.
- Provider readiness remains NOT READY.
- Production pilot remains NOT APPROVED.

## Fail criteria

Any non-local URL, real external endpoint, OAuth credential/token, live Gree+ Cloud call, MQTT operation, device control attempt, production endpoint, production deployment, or command execution is FAIL.

## Troubleshooting

- If the local API is not running, start only the isolated bridge API project.
- If the port is unknown, read the local `dotnet run` output and use the local port shown there.
- If URL validation fails, switch to `http://localhost:<local-port>` or `http://127.0.0.1:<local-port>`.
- Do not use a production endpoint to make the smoke pass.

## Next stage

GREE-ALICE-53 — add Yandex OAuth offline contract skeleton.
