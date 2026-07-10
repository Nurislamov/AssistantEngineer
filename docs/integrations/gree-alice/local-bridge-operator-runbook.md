# Local Bridge Operator Runbook

## Purpose

This runbook explains how an operator can verify the isolated `GreeAliceBridge` locally and offline.

Runbook is offline/local only.
It does not call real Yandex.
It does not implement real OAuth.
It does not use real credentials/tokens.
It does not call live Gree+ Cloud.
It does not use MQTT.
It does not control devices.
It does not deploy to production.

## Current status

Provider readiness remains NOT READY.
Production pilot remains NOT APPROVED.
The bridge remains isolated from `AssistantEngineer.Api`, Telegram runtime, production deployment wiring, and migrations.

## Scope

Use this runbook to verify local build/test health, local smoke harness behavior, fail-closed `/action` behavior, unknown user/device behavior, VRF child-unit exposure, gateway hiding, and masked evidence collection.

## Out of scope

Real Yandex calls, real OAuth, OAuth credentials, live Gree+ Cloud, MQTT, production endpoints, production deployment, device control, and actual command execution against devices are out of scope.

## Prerequisites

- Windows PowerShell or PowerShell 7.
- .NET SDK compatible with `AssistantEngineer.sln`.
- Local checkout of the `AssistantEngineer` repository.
- No real credentials, tokens, account identifiers, device identifiers, or MAC-like values in the repository.

## Repository location

```text
D:\Project\AssistantEngineer
```

Run commands from the repository root unless a command states otherwise.

## Safety boundaries

- Offline/local only.
- No real Yandex calls.
- No real OAuth.
- No real credentials/tokens.
- No live Gree+ Cloud calls.
- No MQTT.
- No device control.
- No production deployment.
- `/action` must remain dry-run fail-closed.
- Unknown users and unknown devices must fail closed.
- VRF child units may be exposed only from offline fixtures.
- VRF/GMV gateways remain hidden/internal by default.

## Local validation commands

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
```

## Local smoke harness commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 -RepoRoot .
```

For a faster repeat after restore/build:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 -RepoRoot . -SkipRestore -SkipBuild
```

Optional localhost HTTP smoke mode:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\integrations\gree-alice\run-local-yandex-provider-smoke.ps1 `
  -RepoRoot . `
  -SkipRestore `
  -SkipBuild `
  -RunHttpSmoke `
  -LocalBaseUrl "http://localhost:<local-port>"
```

HTTP smoke is localhost-only and rejects public hosts, `https`, Yandex/Gree domains, OAuth endpoints, production endpoints, and MQTT endpoints.

## Optional isolated API run command

Use only the isolated bridge API project in local development mode:

```powershell
dotnet run --project .\src\Integrations\GreeAliceBridge\AssistantEngineer.GreeAliceBridge.Api\AssistantEngineer.GreeAliceBridge.Api.csproj
```

Do not run production deployment scripts. Replace `<local-port>` below with the isolated bridge local port used by the developer.

## /health check

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:<local-port>/health"
```

Expected result: local offline health responds without external calls.

## /devices check

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:<local-port>/v1.0/user/devices"
```

Expected result: offline fixture devices are returned for the linked dummy scope; hidden gateway devices are not exposed.

## /query check

```powershell
$body = @{ devices = @(@{ id = "dummy-gree-ac-001" }) } | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri "http://localhost:<local-port>/v1.0/user/devices/query" -ContentType "application/json" -Body $body
```

Expected result: offline fixture state is returned without live Gree+ Cloud, MQTT, or device access.

## /action fail-closed check

```powershell
$body = @{
  devices = @(
    @{
      id = "dummy-gree-ac-001"
      capabilities = @(
        @{ type = "devices.capabilities.on_off"; state = @{ instance = "on"; value = $true } }
      )
    }
  )
} | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri "http://localhost:<local-port>/v1.0/user/devices/action" -ContentType "application/json" -Body $body
```

Expected result: `/action` returns dry-run fail-closed and sends nothing to Gree+ Cloud, MQTT, or devices.

/action returns dry-run fail-closed.

## /unlink check

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:<local-port>/v1.0/user/unlink"
```

Expected result: offline unlink template result; no production account data is touched.

## Unknown user/device checks

Use only dummy/template identifiers. Unknown user scope must not leak the global registry. Unknown device query/action must fail closed.

## VRF child-unit exposure checks

Verify exposed VRF child units appear as user devices with stable dummy Yandex IDs. Verify the VRF/GMV gateway remains hidden/internal by default.

## Evidence collection

Record command output, branch, commit, test result, smoke harness result, local API checks, safety scan result, and final operator decision in a masked evidence file. Store evidence outside the repository or in an ignored local path.

## Pass criteria

- `dotnet restore` PASS.
- `dotnet build` PASS.
- `dotnet test` PASS.
- `git diff --check` PASS.
- Local smoke harness PASS.
- Static safety scans PASS.
- Provider readiness remains NOT READY.
- Production pilot remains NOT APPROVED.
- No real Yandex, OAuth, credentials/tokens, live Gree+ Cloud, MQTT, device control, command execution, production endpoint, or production deployment is used.

## Fail criteria

Any build/test/smoke/safety scan failure is FAIL. Any real external call, real credential/token usage, live Gree+ Cloud call, MQTT operation, device control attempt, command execution against devices, or production deployment attempt is FAIL.

## Forbidden commands

See [local-bridge-forbidden-commands.md](./local-bridge-forbidden-commands.md). Do not run commands that target real Yandex, real Gree+ Cloud, MQTT, device control, production endpoints, or production deployment.

## Secrets and credential rules

Do not add real credentials, tokens, OAuth client secrets, passwords, account identifiers, device identifiers, or MAC-like identifiers. Do not paste secrets into evidence. Evidence must be masked.

## Troubleshooting

- If restore/build/test fails, stop and fix only the local repository issue.
- If static safety scans fail, remove the forbidden local content before continuing.
- If the optional local API does not start, keep the result as FAIL for API checks and do not switch to production endpoints.
- If a port is unknown, inspect local launch output and use only `http://localhost:<local-port>`.

## Next stage

GREE-ALICE-53 — add Yandex OAuth offline contract skeleton.
