# GREE-ALICE-40: isolated staging deploy skeleton

## Purpose

This document defines a future isolated staging deploy skeleton for `AssistantEngineer.GreeAliceBridge.Api`.

It is documentation-only at this stage. It does not deploy anything, does not change production deployment scripts, and does not connect the bridge to production runtime.

## Isolation boundary

The future staging target is only:

```text
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Api
```

The current production `AssistantEngineer.Api` is not modified.

The Telegram runtime is not modified.

Existing production deployment scripts are not modified.

No production stack, VPS service, reverse proxy, database, runtime configuration, or migration is changed in this stage.

## Default safety mode

The staging skeleton remains offline-only and fail-closed by default:

```text
Bridge mode: offline-fixture
Action mode: dry-run fail-closed
Live Gree+ Cloud calls: disabled
MQTT CONNECT: disabled
MQTT SUBSCRIBE: disabled
MQTT PUBLISH: disabled
Device control: disabled
Production runtime wiring: disabled
Deployment changes: none
Migrations: none
```

Do not enable live read, live control, MQTT, or device operations without a separate safety-approved stage.

## Safe local run guide

From the repository root:

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet run --project .\src\Integrations\GreeAliceBridge\AssistantEngineer.GreeAliceBridge.Api\AssistantEngineer.GreeAliceBridge.Api.csproj
```

This local command starts only the isolated `GreeAliceBridge.Api` project.

It must not require:

```text
Gree+ credentials
MQTT broker settings
device keys
tokens
passwords
production connection strings
production deployment scripts
```

## Required validation before any future staging work

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
git diff --stat
```

## Future staging rule

Any future staging deployment skeleton must remain isolated from production `AssistantEngineer.Api`, Telegram runtime, production deployment scripts, production runtime configuration, and migrations until an explicit later safety stage approves otherwise.
