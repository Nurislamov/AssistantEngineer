# ISO 52016 Matrix Solver Stage

## Stage

`ISO52016-MATRIX-SOLVER`

This stage closes the second engineering-core step for the hourly heating/cooling need calculation path.

## Closed work items

| Work item | Scope | Status |
| --- | --- | --- |
| `AE-ISO52016-001` | Node/matrix hourly solver for room/zone sensible heating and cooling need | Closed |
| `AE-GAINS-001` | Internal gains reference data and sensible gain split | Closed |
| `AE-ZONES-001` | Adjacent unconditioned zone temperature model | Closed |

## What is implemented

The implementation adds a separate ISO 52016 Matrix path instead of silently replacing the previous simplified heat-balance path.

Main pieces:

- `IIso52016MatrixHourlySolver` / `Iso52016MatrixHourlySolver`
- V2 node, conductance, boundary and hourly profile contracts
- Internal gain reference data provider
- Adjacent unconditioned zone temperature solver
- Reduced room model adapter from the existing room hourly input profile to Matrix input
- V2 room energy simulation service
- V2 result mapper back to the existing room energy simulation contract
- `Iso52016CalculationPath` switch
- Room facade, building facade, domain facade and application request propagation
- API command and endpoint for explicit simulation engine selection

## Public API path

```http
POST /api/v1/buildings/{buildingId}/energy-analysis/iso52016/simulate
```

Example request body:

```json
{
  "latitudeDegrees": 41.3,
  "longitudeDegrees": 69.2,
  "timeZoneOffset": "05:00:00",
  "weatherYear": 2026,
  "heatBalanceOptions": {
    "initialIndoorTemperatureC": 22,
    "timeStepSeconds": 3600
  }
}
```

## Non-claims

This stage does not claim exact numerical equivalence with StandardReference, EnergyPlus, or ASHRAE 140. It closes an internal architecture and deterministic-test gate for the Matrix solver path.

The existing `EngineeringCoreV1` non-claim about "No full ISO 52016 node/matrix solver equivalence claim" remains valid unless a separate external equivalence/validation stage is completed.

## Verification

Fast targeted verification:

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016Matrix|FullyQualifiedName~Iso52016RoomSimulationFacade|FullyQualifiedName~Iso52016BuildingSimulationFacade"
```

Traceability guard:

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixSolverStageTraceability"
```

Scripted verification:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1
```