namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public static class ExternalReferenceValidationMatrix
{
    public static IReadOnlyList<ExternalReferenceValidationFeature> Features { get; } =
    [
        new(
            Code: "STANDARD_REFERENCE.TRANSMISSION_HEAT_TRANSFER",
            Name: "Transmission heat transfer",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Transmission",
            Notes: "Covered by deterministic fixtures and engine tests. Application pipeline integrated, including explicit ground boundary temperature diagnostics. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.WINDOW_SOLAR_GAINS",
            Name: "Window solar gains",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.BenchmarkCompared,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.SolarGains",
            Notes: "Covered by deterministic fixtures and engine tests. BenchmarkCompared for deterministic window solar gain and night-zero fixtures. Application pipeline integrated; annual climate solar data uses the centralized surface irradiance path and orientation reference fallback is diagnosed. This is not ExternalReferenceCovered."),

        new(
            Code: "STANDARD_REFERENCE.VENTILATION_INFILTRATION_LOADS",
            Name: "Ventilation and infiltration loads",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Ventilation",
            Notes: "Covered by deterministic fixtures and engine tests. Application pipeline integrated; default ACH fallback is documented and diagnosed. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.INTERNAL_GAINS",
            Name: "Internal gains",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.InternalGains",
            Notes: "Covered by deterministic engine tests. Application pipeline integrated; design-point schedule factor 1.0 is documented and hourly schedule expansion remains partial. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.ROOM_HEATING_LOAD",
            Name: "Room heating load",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated with requested/actual method diagnostics. Design-point, not full hourly balance. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.ROOM_COOLING_LOAD",
            Name: "Room cooling load",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated with requested/actual method diagnostics. Design-point, not full hourly balance. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.THERMAL_ZONE_AGGREGATION",
            Name: "Thermal zone aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.FLOOR_AGGREGATION",
            Name: "Floor aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and floor application pipeline tests. Application pipeline integrated. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.BUILDING_AGGREGATION",
            Name: "Building aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and building application pipeline tests. Application pipeline integrated. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.ANNUAL_ENERGY_BALANCE",
            Name: "Annual energy balance",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.BenchmarkCompared,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy",
            Notes: "InternalDeterministicTested by fixtures, engine tests, mapper tests, hourly component tests, and application pipeline adapter tests. Application pipeline integrated with TrueHourlySimulation support when the provider supplies 8760 records, MonthlyBalanceAdapter fallback, hourly record count, and true-8760 flag. BenchmarkCompared for constant hourly deterministic benchmark fixtures and deterministic ventilation split fixture. True hourly source passes available transmission, mechanical ventilation, natural ventilation, separate infiltration, ground, solar and internal gains. This is not ExternalReferenceCovered."),

        new(
            Code: "STANDARD_REFERENCE.SIGNED_COMPONENT_BALANCE",
            Name: "Signed component balance",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.BenchmarkCompared,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy",
            Notes: "InternalDeterministicTested for signed hourly transmission, mechanical ventilation, natural ventilation, aggregate ventilation, infiltration and ground components. BenchmarkCompared for deterministic signed component benchmark fixtures, including separate infiltration and ventilation subcomponent split. This is not ExternalReferenceCovered."),

        new(
            Code: "STANDARD_REFERENCE.DHW_DEMAND",
            Name: "DHW demand",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater",
            Notes: "Covered by deterministic fixtures and service tests; endpoint uses the deterministic facade path. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.SYSTEM_ENERGY",
            Name: "System energy",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy",
            Notes: "Covered by deterministic fixtures and engine tests; system services call SystemEnergyEngine. This is not external equivalence proof."),

        new(
            Code: "STANDARD_REFERENCE.EQUIPMENT_SIZING_INTEGRATION",
            Name: "Equipment sizing integration",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing",
            Notes: "Covered by deterministic fixtures, engine tests, and room equipment application pipeline tests. Application pipeline integrated; cooling and heating are evaluated when catalog capacity fields exist, otherwise heating limitation is diagnosed. This is not external equivalence proof."),

        new(
            Code: "ISO52010.CLIMATE_CONVERSION",
            Name: "ISO 52010 external climate conversion",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Solar position has deterministic tests and is used by the weather-solar surface irradiance path. Broader external climate conversion remains Partial and is not ExternalReferenceCovered."),

        new(
            Code: "ISO52010.SURFACE_IRRADIANCE",
            Name: "Solar irradiance on tilted and oriented surfaces",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "InternalDeterministicTested for isotropic sky surface irradiance, orientation/tilt behavior, horizontal midday irradiance, night clamping to zero, and weather-solar diagnostics. BenchmarkCompared for deterministic surface night-zero fixture. External equivalence remains not covered."),

        new(
            Code: "WEATHER.EPW",
            Name: "EPW weather input",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Infrastructure.Integrations",
            Notes: "Requires a normalized weather dataset for 8760 hours."),

        new(
            Code: "WEATHER.PVGIS",
            Name: "PVGIS weather input",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Infrastructure.Integrations",
            Notes: "Provider separation from the calculation core remains partial."),

        new(
            Code: "ISO52016.HOURLY_HEATING_NEED",
            Name: "ISO 52016 sensible heating need hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Main calculation path. Must not be replaced by a design-day fallback."),

        new(
            Code: "ISO52016.HOURLY_COOLING_NEED",
            Name: "ISO 52016 sensible cooling need hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Main calculation path. Requires 8760 hourly results."),

        new(
            Code: "ISO52016.MONTHLY_HEATING_COOLING_NEED",
            Name: "ISO 52016 sensible heating and cooling need monthly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Monthly results should be aggregated from hourly values or aligned with the monthly method."),

        new(
            Code: "ISO52016.INTERNAL_TEMPERATURE_HOURLY",
            Name: "ISO 52016 internal temperature hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016",
            Notes: "OperativeTemperatureC exists in the contract, but still needs reference tests."),

        new(
            Code: "ISO52016.SENSIBLE_LOAD_HOURLY",
            Name: "ISO 52016 sensible heating and cooling load hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016",
            Notes: "HeatingLoadW and CoolingLoadW exist in the hourly contract, but still need reference fixtures."),

        new(
            Code: "ISO52016.THERMAL_ZONES",
            Name: "ISO 52016 thermal zone calculation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "Thermal zones exist, but the computational model still needs reference validation."),

        new(
            Code: "ISO52016.MULTI_ZONE",
            Name: "Multi-zone calculation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Requires tests for coupled and uncoupled zones."),

        new(
            Code: "ISO52016.ADJACENT_HEATED_ZONE",
            Name: "Adjacent heated zones and adiabatic walls",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "For heated adjacent zones, the separating wall should be treated as adiabatic."),

        new(
            Code: "ISO52016.ADJACENT_NON_HEATED_ZONE",
            Name: "Adjacent non-heated zones",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "Requires adjacent non-heated zone temperature handling and adjusted coefficient support."),

        new(
            Code: "DHW.EN12831_3",
            Name: "Domestic hot water volume and energy need according to EN 12831-3",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater",
            Notes: "First reproduce reference behavior, then extend."),

        new(
            Code: "PRIMARY_ENERGY.EN15316_1",
            Name: "Primary energy according to EN 15316-1",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: ExternalReferenceValidationPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Performance",
            Notes: "Requires delivered/final/primary energy with carrier factors."),

        new(
            Code: "LATENT.ENERGY_NEED",
            Name: "Latent energy need for humidification/dehumidification",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: ExternalReferenceValidationPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Out of current validation scope."),

        new(
            Code: "LATENT.MOISTURE_LOAD",
            Name: "Moisture and latent heat load",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: ExternalReferenceValidationPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Out of current validation scope."),

        new(
            Code: "SUPPLY_AIR.HUMIDIFICATION_CONDITIONS",
            Name: "Supply-air humidification and dehumidification conditions",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: ExternalReferenceValidationPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Out of current validation scope."),
    ];
}
