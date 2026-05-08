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

---

## Work item

- `AE-ZONES-STANDARDS-001B`

## Touched contracts

- `Application/Abstractions/Topology/IThermalZoneBoundaryCalculator.cs`
- `Application/Contracts/Topology/ThermalZoneBoundaryCalculationInput.cs`
- `Application/Contracts/Topology/ThermalSurfaceBoundaryCalculationResult.cs`
- `Application/Contracts/Topology/ThermalRoomBoundaryCalculationResult.cs`
- `Application/Contracts/Topology/ThermalZoneBoundaryCalculationResult.cs`
- `Application/Contracts/Topology/BuildingThermalBoundaryCalculationResult.cs`

## Touched services

- `Application/Services/Topology/ThermalZoneBoundaryCalculator.cs`
- `Composition/ThermalTopologyRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalZoneBoundaryCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalZoneBoundaryAggregationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/ThermalZones/ThermalZoneBoundaryCalculatorArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Boundary-integration non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic internal engineering boundary integration only.

## Next stages unlocked

- `AE-GROUND-ISO13370-001A`
- `AE-VENT-EN16798-001A`
- future ISO52016 boundary coupling refinement

---

## Work item

- `AE-GROUND-ISO13370-001A`

## Touched contracts

- `Application/Contracts/Ground/GroundContactKind.cs`
- `Application/Contracts/Ground/GroundInsulationPlacement.cs`
- `Application/Contracts/Ground/GroundContactGeometry.cs`
- `Application/Contracts/Ground/GroundSoilProperties.cs`
- `Application/Contracts/Ground/GroundClimateInput.cs`
- `Application/Contracts/Ground/GroundBoundaryCalculationInput.cs`
- `Application/Contracts/Ground/GroundBoundaryCalculationResult.cs`
- `Application/Contracts/Ground/GroundBoundaryInputValidationResult.cs`
- `Application/Contracts/Ground/GroundTemperatureProfileResult.cs`
- `Application/Abstractions/Ground/IGroundGeometryNormalizer.cs`
- `Application/Abstractions/Ground/IGroundBoundaryInputValidator.cs`
- `Application/Abstractions/Ground/IGroundTemperatureProfileProvider.cs`
- `Application/Abstractions/Ground/IGroundBoundaryCalculator.cs`

## Touched services

- `Application/Services/Ground/GroundCalculationDiagnosticsFactory.cs`
- `Application/Services/Ground/GroundGeometryNormalizer.cs`
- `Application/Services/Ground/GroundBoundaryInputValidator.cs`
- `Application/Services/Ground/GroundTemperatureProfileProvider.cs`
- `Application/Services/Ground/GroundBoundaryCalculator.cs`
- `Composition/GroundRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundGeometryNormalizerTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundBoundaryInputValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundTemperatureProfileProviderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundBoundaryCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundCalculationArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Ground-lane non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic internal engineering ground-boundary integration only.

## Next stage

- `AE-GROUND-ISO13370-001B`

---

## Work item

- `AE-GROUND-ISO13370-001B`

## Touched contracts

- `Application/Contracts/Ground/GroundSurfaceMetadata.cs`
- `Application/Contracts/Ground/BuildingGroundBoundaryCalculationInput.cs`
- `Application/Contracts/Ground/GroundSurfaceBoundaryCalculationResult.cs`
- `Application/Contracts/Ground/BuildingGroundBoundaryCalculationResult.cs`
- `Application/Contracts/Ground/GroundBoundaryTemperatureLookup.cs`
- `Application/Contracts/Ground/ThermalZoneGroundBoundaryInputAdapterResult.cs`
- `Application/Contracts/Ground/GroundBoundaryIso52016BoundaryProfileMappingResult.cs`
- `Application/Abstractions/Ground/IGroundBoundaryTopologyMapper.cs`
- `Application/Abstractions/Ground/IBuildingGroundBoundaryCalculator.cs`
- `Application/Abstractions/Ground/IGroundBoundaryTemperatureLookupBuilder.cs`
- `Application/Abstractions/Ground/IThermalZoneGroundBoundaryInputAdapter.cs`
- `Application/Abstractions/Ground/IGroundBoundaryToIso52016BoundaryProfileMapper.cs`

## Touched services

- `Application/Services/Ground/GroundBoundaryTopologyMapper.cs`
- `Application/Services/Ground/BuildingGroundBoundaryCalculator.cs`
- `Application/Services/Ground/GroundBoundaryTemperatureLookupBuilder.cs`
- `Application/Services/Ground/ThermalZoneBoundaryGroundTemperatureAdapter.cs`
- `Application/Services/Ground/GroundBoundaryToIso52016BoundaryProfileMapper.cs`
- `Composition/GroundCalculationRegistration.cs`
- updated `Composition/GroundRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundBoundaryTopologyMapperTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/BuildingGroundBoundaryCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundBoundaryTemperatureLookupBuilderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/ThermalZoneBoundaryGroundTemperatureAdapterTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundBoundaryToIso52016BoundaryProfileMapperTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ground/GroundIntegrationArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Ground-topology integration non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic standard-inspired application integration only.

## Next stage

- `AE-VENT-EN16798-001A`

---

## Work item

- `AE-VENT-EN16798-001A`

## Touched contracts

- `Application/Contracts/Ventilation/NaturalVentilationOpeningType.cs`
- `Application/Contracts/Ventilation/NaturalVentilationFlowConfiguration.cs`
- `Application/Contracts/Ventilation/NaturalVentilationOpeningOperation.cs`
- `Application/Contracts/Ventilation/NaturalVentilationOpeningGeometry.cs`
- `Application/Contracts/Ventilation/NaturalVentilationEnvironment.cs`
- `Application/Contracts/Ventilation/NaturalVentilationCalculationInput.cs`
- `Application/Contracts/Ventilation/NaturalVentilationOpeningResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationCalculationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationInputValidationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationPressureResult.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationOpeningGeometryNormalizer.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationInputValidator.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationPressureCalculator.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationAirflowCalculator.cs`

## Touched services

- `Application/Services/Ventilation/NaturalVentilationDiagnosticsFactory.cs`
- `Application/Services/Ventilation/NaturalVentilationOpeningGeometryNormalizer.cs`
- `Application/Services/Ventilation/NaturalVentilationInputValidator.cs`
- `Application/Services/Ventilation/NaturalVentilationPressureCalculator.cs`
- `Application/Services/Ventilation/NaturalVentilationAirflowCalculator.cs`
- `Composition/NaturalVentilationRegistration.cs`
- updated `Composition/VentilationRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationOpeningGeometryNormalizerTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationInputValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationPressureCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationAirflowCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Natural ventilation foundation non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic standard-inspired engineering calculation only.

## Next stage

- `AE-VENT-EN16798-001B`

---

## Work item

- `AE-VENT-EN16798-001B`

## Touched contracts

- `Application/Contracts/Ventilation/NaturalVentilationControlMode.cs`
- `Application/Contracts/Ventilation/NaturalVentilationNightVentilationMode.cs`
- `Application/Contracts/Ventilation/NaturalVentilationOpeningControlRule.cs`
- `Application/Contracts/Ventilation/NaturalVentilationHourlyControlContext.cs`
- `Application/Contracts/Ventilation/NaturalVentilationOpeningOperationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationControlEvaluationInput.cs`
- `Application/Contracts/Ventilation/NaturalVentilationControlEvaluationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationControlRuleValidationResult.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationControlRuleValidator.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationOpeningControlEvaluator.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationOpeningFractionProfileBuilder.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationControlledAirflowInputBuilder.cs`

## Touched services

- `Application/Services/Ventilation/NaturalVentilationControlRuleValidator.cs`
- `Application/Services/Ventilation/NaturalVentilationOpeningControlEvaluator.cs`
- `Application/Services/Ventilation/NaturalVentilationOpeningFractionProfileBuilder.cs`
- `Application/Services/Ventilation/NaturalVentilationControlledAirflowInputBuilder.cs`
- updated `Composition/NaturalVentilationRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationControlRuleValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationOpeningControlEvaluatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationOpeningFractionProfileBuilderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationControlledAirflowInputBuilderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationControlArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Natural ventilation controls non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic standard-inspired engineering calculation only.

## Next stage

- `AE-VENT-EN16798-001C`

---

## Work item

- `AE-VENT-EN16798-001C`

## Touched contracts

- `Application/Contracts/Ventilation/NaturalVentilationZoneIntegrationInput.cs`
- `Application/Contracts/Ventilation/NaturalVentilationHourlyZoneEnvironment.cs`
- `Application/Contracts/Ventilation/NaturalVentilationHourlyOpeningCalculationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationHourlyRoomResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationHourlyZoneResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationZoneIntegrationResult.cs`
- `Application/Contracts/Ventilation/NaturalVentilationZoneIntegrationValidationResult.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationZoneIntegrationValidator.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationHourlyInputBuilder.cs`
- `Application/Abstractions/Ventilation/INaturalVentilationZoneLoadCalculator.cs`

## Touched services

- `Application/Services/Ventilation/NaturalVentilationZoneIntegrationValidator.cs`
- `Application/Services/Ventilation/NaturalVentilationHourlyInputBuilder.cs`
- `Application/Services/Ventilation/NaturalVentilationZoneLoadCalculator.cs`
- updated `Composition/NaturalVentilationRegistration.cs`

## Tests

- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationZoneIntegrationValidatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationHourlyInputBuilderTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationZoneLoadCalculatorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Ventilation/NaturalVentilationZoneIntegrationArchitectureTests.cs`
- updated DI coverage in `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

## Claim boundaries

Natural ventilation zone/hourly integration non-claims:
- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

This stage provides deterministic standard-inspired application integration only.

## Next stage

- `AE-DHW-ISO12831-001A`
