# Standards Foundation Manifest

## Work item

- `AE-STANDARDS-FOUNDATION-001`

## Touched contracts

- `Application/Contracts/Standards/*`
- `Application/Contracts/Topology/*`

Key additions:
- `StandardCalculationFamily`
- `StandardCalculationStage`
- `StandardCalculationMode`
- `StandardClaimBoundary`
- `StandardCalculationDisclosure`
- `StandardCalculationDiagnostic`
- `EngineeringUnit`
- `EngineeringQuantity`
- `EngineeringUnitConverter`
- `AnnualProfileShapeValidationResult`
- `ThermalBoundaryKind`
- `ThermalTopologySurface`
- `ThermalTopologyRoom`
- `ThermalTopologyZone`
- `BuildingThermalTopology`
- `ThermalBoundaryMappingResult<TBoundary>`

## Touched services

- `Application/Services/Standards/StandardCalculationDisclosureFactory.cs`
- `Application/Services/Common/Profiles/AnnualProfileShapeValidator.cs`
- `Application/Mappers/ThermalBoundaryKindMapper.cs`
- `Composition/StandardsFoundationRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/StandardsFoundation/StandardDisclosureFactoryTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/StandardsFoundation/EngineeringUnitConversionTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/StandardsFoundation/AnnualProfileShapeValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/StandardsFoundation/ThermalBoundaryKindMapperTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/StandardsFoundation/StandardsFoundationArchitectureTests.cs`
- updated DI lifetime coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Foundation-level forbidden claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This manifest describes internal deterministic engineering contract readiness only. It is not a standards certification statement.

## Next stages unlocked

- `AE-ZONES-STANDARDS-001`
- `AE-GROUND-ISO13370-001`
- `AE-VENT-EN16798-001`
- `AE-DHW-ISO12831-001`
- `AE-SYS-EN15316-001`
