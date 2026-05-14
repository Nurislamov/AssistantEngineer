# Calculations-Contracts to Buildings-Domain Boundary Debt

## Context

Current modularity debt: parts of `AssistantEngineer.Modules.Calculations.Application.Contracts` still reference
`AssistantEngineer.Modules.Buildings.Domain`.

Target boundary is:

- `Buildings.Application.Contracts` exposes snapshots and projections.
- `Calculations.Application` consumes calculation input snapshots.
- `Calculations.Application.Contracts` must not expose `Buildings.Domain` entities.

This document captures the baseline debt and the guard policy for incremental cleanup.

## Baseline Debt (Temporary Allowlist)

The following contract files currently reference `AssistantEngineer.Modules.Buildings.Domain` and are temporarily
allowlisted by architecture guard.

1. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingDomainSimulationFacadeRequest.cs`
   Uses: `Buildings.Domain.Climate`, `Buildings.Domain.Entities`
2. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingDomainSimulationFacadeResult.cs`
   Uses: `Buildings.Domain.Entities`
3. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingSimulationFacadeRequest.cs`
   Uses: `Buildings.Domain.Climate`, `Buildings.Domain.Entities`
4. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyRoomWindowSolarGainRecord.cs`
   Uses: `Buildings.Domain.Enums`
5. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyWeatherSolarRecord.cs`
   Uses: `Buildings.Domain.Enums`
6. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyWindowSolarGainRecord.cs`
   Uses: `Buildings.Domain.Enums`
7. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016RoomEnergySimulationBuildRequest.cs`
   Uses: `Buildings.Domain.Entities`
8. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016RoomSimulationFacadeRequest.cs`
   Uses: `Buildings.Domain.Climate`, `Buildings.Domain.Entities`
9. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WeatherSolarContextRequest.cs`
   Uses: `Buildings.Domain.Climate`
10. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainInput.cs`
    Uses: `Buildings.Domain.Enums`
11. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainProfile.cs`
    Uses: `Buildings.Domain.Enums`
12. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainProfileRequest.cs`
    Uses: `Buildings.Domain.Enums`
13. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainRequest.cs`
    Uses: `Buildings.Domain.Enums`
14. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainResult.cs`
    Uses: `Buildings.Domain.Enums`
15. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Validation/BuildingInput/BuildingInputValidationRequest.cs`
    Uses: `Buildings.Domain.Entities`
16. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Weather/AnnualWeatherNormalizationRequest.cs`
    Uses: `Buildings.Domain.Climate`
17. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/WeatherSolar/WeatherSolarSurface.cs`
    Uses: `Buildings.Domain.Enums`
18. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/WeatherSolar/WeatherSolarSurfaceCodes.cs`
    Uses: `Buildings.Domain.Enums`

## Guard Policy

- New references from `Calculations.Application.Contracts` to `Buildings.Domain` are forbidden.
- Existing references above are baseline debt only and may be reduced incrementally.
- Any new violating file outside this baseline must fail tests.

## P1 Backlog Item

**P1: Migrate contract debt files to snapshots/projections**

- Replace `Buildings.Domain.*` dependencies in the listed contract files with dedicated boundary snapshots/projections from `Buildings.Application.Contracts`.
- Keep `Calculations.Application.Contracts` DTO-facing and domain-entity-free.
- Retire baseline allowlist entries as files are migrated.
