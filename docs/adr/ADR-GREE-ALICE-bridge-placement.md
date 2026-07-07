# ADR: GREE-ALICE bridge placement

## Status

Accepted.

## Context

AssistantEngineer already contains production backend services, Telegram bot logic, engineering calculations, equipment diagnostics, deployment scripts, and a growing test suite.

The GREE-ALICE work has a different operational profile:

- Yandex Smart Home provider API.
- Gree+ / Gree Cloud account integration.
- Gree Cloud REST and MQTT communication.
- Device mapping for split AC and VRF/GMV systems.
- User account, token, and secret storage.
- Real equipment command execution.

Mixing this logic directly into the current production `AssistantEngineer.Api` would increase risk for the stable Telegram/diagnostics/runtime path.

## Decision

Implement GREE-ALICE as a separate deployable service / bounded context inside the existing AssistantEngineer monorepo and solution.

The service must live beside the existing product modules, not inside the current production API or Telegram runtime.

Target future layout:

```text
src/Integrations/GreeAliceBridge/
tests/Integrations/AssistantEngineer.GreeAliceBridge.Tests/
docs/integrations/gree-alice/
```

The first stage is documentation-only and does not add runtime code.

## Primary architecture

```text
Alice / Yandex Smart Home
        ↓
GreeAliceBridge on VPS
        ↓
Gree Cloud / Gree+
        ↓
Gree split AC and Gree VRF / GMV
```

## Scope

The module must support:

- Alice / Yandex Smart Home control.
- No required local server on the object.
- Gree+ / Gree Cloud connected devices.
- Split AC devices.
- VRF/GMV systems where Gree Cloud exposes controllable gateways or child indoor units.

## Non-goals for the first stages

The first stages do not include:

- Production deployment changes.
- Modifying Telegram bot runtime.
- Modifying equipment diagnostics catalog.
- Adding EF migrations to the main backend.
- Adding Gree+ credentials to the repository.
- Claiming official Gree API support.

## Fallback paths

Fallback paths are allowed only if the cloud path fails:

1. Local Gree Wi-Fi UDP protocol.
2. Local agent.
3. VPN/local bridge.
4. Modbus/BACnet gateway.
5. BMS integration.

## Consequences

Positive:

- Keeps the main AssistantEngineer runtime stable.
- Preserves one repository and one project history.
- Enables shared engineering style, tests, CI, documentation, and deployment patterns.
- Keeps the future option to extract the module into a separate repository.

Trade-offs:

- The solution may grow with another deployable service.
- Architecture tests may need explicit boundary registration when runtime projects are added.
- Gree Cloud behavior must be validated before investing in the full Alice provider.

## Protected areas

Do not modify the following areas in documentation/probe stages:

```text
src/Backend/AssistantEngineer.Api/
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/
src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations/
data/equipment-diagnostics/
deploy/
scripts/deployment/
```

## Validation

Every stage must end with at least:

```powershell
git diff --check
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
```
