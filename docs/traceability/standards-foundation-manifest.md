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

---

## Work item

- `AE-ZONES-STANDARDS-001A`

## Touched contracts

- `Application/Abstractions/Topology/*`
- `Application/Contracts/Topology/*`

Key additions:
- `IThermalTopologyBuilder`
- `IThermalTopologyValidator`
- `IThermalBoundaryConditionResolver`
- `ThermalTopologyBuildInput`
- `ThermalTopologyZoneInput`
- `ThermalTopologyRoomInput`
- `ThermalTopologySurfaceInput`
- `ThermalTopologyValidationResult`
- `ThermalBoundaryResolutionResult`

## Touched services

- `Application/Services/Topology/ThermalTopologyBuilder.cs`
- `Application/Services/Topology/ThermalTopologyValidator.cs`
- `Application/Services/Topology/ThermalBoundaryConditionResolver.cs`
- `Application/Services/Topology/ThermalTopologyDiagnosticsFactory.cs`
- `Composition/ThermalTopologyRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalTopologyBuilderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalTopologyValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalBoundaryConditionResolverTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalTopologyArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Topology-foundation non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage defines deterministic topology contracts and diagnostics only.

## Next stage

- `AE-ZONES-STANDARDS-001B`
