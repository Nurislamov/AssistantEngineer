namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

public static class EnergyCalculationParityMatrix
{
    public static IReadOnlyList<EnergyCalculationParityFeature> Features { get; } =
    [
        new(
            Code: "ENERGY_CALCULATION_PARITY.TRANSMISSION_HEAT_TRANSFER",
            Name: "Transmission heat transfer",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Transmission",
            Notes: "Covered by deterministic fixtures and engine tests. Application pipeline integrated, including explicit ground boundary temperature diagnostics. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.WINDOW_SOLAR_GAINS",
            Name: "Window solar gains",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.SolarGains",
            Notes: "Covered by deterministic fixtures and engine tests. Application pipeline integrated; annual/weather solar source is preferred and orientation reference fallback is diagnosed. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.VENTILATION_INFILTRATION_LOADS",
            Name: "Ventilation and infiltration loads",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Ventilation",
            Notes: "Covered by deterministic fixtures and engine tests. Application pipeline integrated; default ACH fallback is documented and diagnosed. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.INTERNAL_GAINS",
            Name: "Internal gains",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.InternalGains",
            Notes: "Covered by deterministic engine tests. Application pipeline integrated; design-point schedule factor 1.0 is documented and hourly schedule expansion remains partial. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.ROOM_HEATING_LOAD",
            Name: "Room heating load",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated with requested/actual method diagnostics. Design-point, not full hourly balance. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.ROOM_COOLING_LOAD",
            Name: "Room cooling load",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated with requested/actual method diagnostics. Design-point, not full hourly balance. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.THERMAL_ZONE_AGGREGATION",
            Name: "Thermal zone aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and application pipeline tests. Application pipeline integrated. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.FLOOR_AGGREGATION",
            Name: "Floor aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and floor application pipeline tests. Application pipeline integrated. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.BUILDING_AGGREGATION",
            Name: "Building aggregation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Aggregation",
            Notes: "Covered by deterministic fixtures, engine tests, and building application pipeline tests. Application pipeline integrated. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.ANNUAL_ENERGY_BALANCE",
            Name: "Annual energy balance",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.BenchmarkCompared,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy",
            Notes: "InternalDeterministicTested by fixtures, engine tests, mapper tests, hourly component tests, and application pipeline adapter tests. Application pipeline integrated with TrueHourlySimulation support when the provider supplies 8760 records, MonthlyBalanceAdapter fallback, hourly record count, and true-8760 flag. BenchmarkCompared for constant hourly deterministic benchmark fixtures. True hourly source passes available transmission, mechanical/natural ventilation, separate infiltration, ground, solar and internal gains. This is not ExternalParityCovered."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.SIGNED_COMPONENT_BALANCE",
            Name: "Signed component balance",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.BenchmarkCompared,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy",
            Notes: "InternalDeterministicTested for signed hourly transmission, ventilation, infiltration and ground components. BenchmarkCompared for deterministic signed component benchmark fixtures, including separate infiltration. This is not ExternalParityCovered."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.DHW_DEMAND",
            Name: "DHW demand",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater",
            Notes: "Covered by deterministic fixtures and service tests; endpoint uses the deterministic facade path. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.SYSTEM_ENERGY",
            Name: "System energy",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy",
            Notes: "Covered by deterministic fixtures and engine tests; system services call SystemEnergyEngine. This is not external parity proof."),

        new(
            Code: "ENERGY_CALCULATION_PARITY.EQUIPMENT_SIZING_INTEGRATION",
            Name: "Equipment sizing integration",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.InternalDeterministicTested,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing",
            Notes: "Covered by deterministic fixtures, engine tests, and room equipment application pipeline tests. Application pipeline integrated; cooling and heating are evaluated when catalog capacity fields exist, otherwise heating limitation is diagnosed. This is not external parity proof."),

        new(
            Code: "ISO52010.CLIMATE_CONVERSION",
            Name: "ISO 52010 external climate conversion",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Нужно выделить отдельный ISO 52010 weather/solar layer."),

        new(
            Code: "ISO52010.SURFACE_IRRADIANCE",
            Name: "Solar irradiance on tilted and oriented surfaces",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Нужны reference tests для N/E/S/W/horizontal surfaces."),

        new(
            Code: "WEATHER.EPW",
            Name: "EPW weather input",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Infrastructure.Integrations",
            Notes: "Нужен нормализованный weather dataset на 8760 часов."),

        new(
            Code: "WEATHER.PVGIS",
            Name: "PVGIS weather input",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Infrastructure.Integrations",
            Notes: "Нужно отделить provider от расчётного ядра."),

        new(
            Code: "ISO52016.HOURLY_HEATING_NEED",
            Name: "ISO 52016 sensible heating need hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Главный расчётный путь. Не должен подменяться design-day fallback."),

        new(
            Code: "ISO52016.HOURLY_COOLING_NEED",
            Name: "ISO 52016 sensible cooling need hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Главный расчётный путь. Нужны 8760 hourly results."),

        new(
            Code: "ISO52016.MONTHLY_HEATING_COOLING_NEED",
            Name: "ISO 52016 sensible heating and cooling need monthly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Monthly results должны быть агрегированы из hourly или соответствовать monthly method."),

        new(
            Code: "ISO52016.INTERNAL_TEMPERATURE_HOURLY",
            Name: "ISO 52016 internal temperature hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016",
            Notes: "OperativeTemperatureC уже есть в contract, но нужен reference test."),

        new(
            Code: "ISO52016.SENSIBLE_LOAD_HOURLY",
            Name: "ISO 52016 sensible heating and cooling load hourly",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016",
            Notes: "HeatingLoadW и CoolingLoadW есть в hourly contract, но нужны эталонные fixtures."),

        new(
            Code: "ISO52016.THERMAL_ZONES",
            Name: "ISO 52016 thermal zone calculation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P0,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "Thermal zones уже есть, но нужно сверить расчётную модель."),

        new(
            Code: "ISO52016.MULTI_ZONE",
            Name: "Multi-zone calculation",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Iso52016",
            Notes: "Нужны tests для coupled / uncoupled zones."),

        new(
            Code: "ISO52016.ADJACENT_HEATED_ZONE",
            Name: "Adjacent heated zones and adiabatic walls",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "Для heated adjacent zones separating wall должен быть adiabatic."),

        new(
            Code: "ISO52016.ADJACENT_NON_HEATED_ZONE",
            Name: "Adjacent non-heated zones",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Buildings.Domain.Entities / Calculations",
            Notes: "Нужно рассчитывать температуру non-heated zone и adjusted coefficient."),

        new(
            Code: "DHW.EN12831_3",
            Name: "Domestic hot water volume and energy need according to EN 12831-3",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater",
            Notes: "Сначала повторить reference behaviour, потом расширять."),

        new(
            Code: "PRIMARY_ENERGY.EN15316_1",
            Name: "Primary energy according to EN 15316-1",
            ReferenceStatus: ReferenceFeatureStatus.Implemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.Partial,
            Priority: EnergyCalculationParityPriority.P1,
            AssistantEngineerArea: "AssistantEngineer.Modules.Calculations.Application.Services.Performance",
            Notes: "Нужны delivered/final/primary energy и carrier factors."),

        new(
            Code: "LATENT.ENERGY_NEED",
            Name: "Latent energy need for humidification/dehumidification",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: EnergyCalculationParityPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Не входит в текущий parity target."),

        new(
            Code: "LATENT.MOISTURE_LOAD",
            Name: "Moisture and latent heat load",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: EnergyCalculationParityPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Не входит в текущий parity target."),

        new(
            Code: "SUPPLY_AIR.HUMIDIFICATION_CONDITIONS",
            Name: "Supply-air humidification and dehumidification conditions",
            ReferenceStatus: ReferenceFeatureStatus.NotImplemented,
            AssistantEngineerStatus: AssistantEngineerFeatureStatus.OutOfScope,
            Priority: EnergyCalculationParityPriority.P3,
            AssistantEngineerArea: "Not planned",
            Notes: "Не входит в текущий parity target.")
    ];
}
