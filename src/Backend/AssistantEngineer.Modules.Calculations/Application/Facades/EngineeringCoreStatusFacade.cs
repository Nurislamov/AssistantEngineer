using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class EngineeringCoreStatusFacade : IEngineeringCoreStatusFacade
{
    private const string ClosedV1 = "ClosedV1";
    private const string OutOfScopeV1 = "OutOfScopeV1";
    private const string PlannedValidation = "PlannedValidation";

    public Result<EngineeringCoreV1StatusResponse> GetEngineeringCoreV1Status()
    {
        var formulaGates = FormulaGates();

        var response = new EngineeringCoreV1StatusResponse(
            CoreName: "AssistantEngineer Engineering Core",
            Version: "v1",
            Status: ClosedV1,
            FormulaGatesClosed: formulaGates.All(gate => gate.Status == ClosedV1),
            Weather8760GatesClosed: formulaGates
                .Where(gate => gate.CalculationId is "WEATHER.EPW_8760" or "WEATHER.PVGIS_8760")
                .All(gate => gate.Status == ClosedV1),
            AnnualHourly8760GateClosed: formulaGates
                .Single(gate => gate.CalculationId == "HVAC.ANNUAL_ENERGY.HOURLY_KWH")
                .Status == ClosedV1,
            SuccessfulResultsMustNotContainErrorDiagnostics: true,
            FormulaGates: formulaGates,
            ExplicitNonClaims:
            [
                "No exact StandardReference numerical equivalence claim.",
                "No exact EnergyPlus numerical equivalence claim.",
                "No ASHRAE 140 / BESTEST-style validation anchor coverage claim.",
                "No full ISO 52016 node/matrix solver equivalence claim.",
                "No full ISO 52010 climate conversion equivalence claim.",
                "No full ISO 13370 implementation claim.",
                "No full EN 15316 generation/distribution/storage/emission chain claim.",
                "No full coupled multi-zone heat-balance simulation claim.",
                "No latent/moisture/humidity calculation claim.",
                "SystemEnergyEngine compatibility path remains default.",
                "EN15316-inspired modular chain is opt-in.",
                "ISO12831-3-inspired DHW path is opt-in."
            ],
            OutOfScopeV1:
            [
                "HVAC.LATENT_LOAD",
                "HVAC.MOISTURE_BALANCE",
                "Humidification/dehumidification conditions",
                "Detailed psychrometric supply-air treatment",
                "Detailed HVAC plant simulation"
            ],
            PlannedValidation:
            [
                "VALIDATION.ENERGYPLUS_ASHRAE140"
            ],
            RequiredAnnual8760Flags:
            [
                "EnergyDataSource = TrueHourlySimulation",
                "IsTrueHourly8760 = true",
                "HourlyRecordCount = 8760"
            ],
            DocumentationFiles:
            [
                "docs/calculations/EngineeringCoreV1Scope.md",
                "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
                "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
            ]);

        return Result<EngineeringCoreV1StatusResponse>.Success(response);
    }
    public Result<EngineeringCoreV1DiagnosticsCatalogResponse> GetEngineeringCoreV1DiagnosticsCatalog()
    {
        var response = new EngineeringCoreV1DiagnosticsCatalogResponse(
            CatalogName: "Engineering Core V1 Diagnostics Catalog",
            Version: "v1",
            Status: ClosedV1,
            Rules: new EngineeringCoreV1DiagnosticsRules(
                Error: "Invalid mandatory input. Calculation must fail.",
                Warning: "Fallback, simplification, missing optional assumption or partial source. Calculation may succeed.",
                Info: "Method, source, status or metadata. Calculation may succeed.",
                SuccessRule: "A successful calculation result must not contain CalculationDiagnosticSeverity.Error."),
            Diagnostics:
            [
                new(
                    Code: "AnnualEnergy.InvalidArea",
                    Severity: "Error",
                    Category: "AnnualEnergy",
                    UserMessage: "Building area must be greater than zero for EUI calculation.",
                    UserAction: "Enter a positive building area before calculating annual EUI.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.NoHourlyInputs",
                    Severity: "Error",
                    Category: "AnnualEnergy",
                    UserMessage: "At least one hourly energy balance input is required.",
                    UserAction: "Provide hourly records or run a calculation path that generates hourly inputs.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.InvalidHourDuration",
                    Severity: "Error",
                    Category: "AnnualEnergy",
                    UserMessage: "Hour duration must be greater than zero.",
                    UserAction: "Check the hourly weather/profile source and remove invalid hour durations.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.InvalidMonth",
                    Severity: "Error",
                    Category: "AnnualEnergy",
                    UserMessage: "Month must be between 1 and 12.",
                    UserAction: "Check hourly profile month mapping.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.Not8760",
                    Severity: "Warning",
                    Category: "AnnualEnergy",
                    UserMessage: "Hourly input count is not 8760; annual totals use the supplied calculation period.",
                    UserAction: "Do not present this result as true hourly annual 8760 simulation unless HourlyRecordCount is 8760.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.SyntheticWeather",
                    Severity: "Warning",
                    Category: "AnnualEnergy",
                    UserMessage: "Synthetic weather profile was used for annual energy balance.",
                    UserAction: "Use EPW or PVGIS normalized weather for weather-driven annual analysis.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.MonthlyBalanceAdapter",
                    Severity: "Warning",
                    Category: "AnnualEnergy",
                    UserMessage: "Annual energy balance uses representative monthly records generated from monthly balances; this is not a true hourly 8760 simulation.",
                    UserAction: "Show this as an adapted monthly balance, not true hourly 8760 simulation.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "AnnualEnergy.TrueHourlySimulationUsed",
                    Severity: "Info",
                    Category: "AnnualEnergy",
                    UserMessage: "Annual energy balance was calculated from true hourly simulation records.",
                    UserAction: "This supports true hourly annual reporting when the 8760 flags are also satisfied.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "SolarWeather.SyntheticWeatherUsed",
                    Severity: "Warning",
                    Category: "Weather",
                    UserMessage: "Synthetic weather profile was used; solar/weather source data is representative rather than true hourly weather.",
                    UserAction: "Treat the result as representative, not a true external weather simulation.",
                    ClosedV1Gate: "HVAC.ANNUAL_ENERGY.HOURLY_KWH"),

                new(
                    Code: "SystemEnergy.InvalidCoolingCop",
                    Severity: "Error",
                    Category: "SystemEnergy",
                    UserMessage: "Cooling COP must be greater than zero.",
                    UserAction: "Enter a positive cooling COP.",
                    ClosedV1Gate: "HVAC.SYSTEM_ENERGY.SIMPLIFIED"),

                new(
                    Code: "SystemEnergy.HeatingAssumptionMissing",
                    Severity: "Warning",
                    Category: "SystemEnergy",
                    UserMessage: "No heating efficiency or COP was supplied; useful heating energy was carried through as final energy.",
                    UserAction: "Provide heating efficiency or COP for final energy conversion.",
                    ClosedV1Gate: "HVAC.SYSTEM_ENERGY.SIMPLIFIED"),

                new(
                    Code: "SystemEnergy.CoolingAssumptionMissing",
                    Severity: "Warning",
                    Category: "SystemEnergy",
                    UserMessage: "No cooling COP was supplied; useful cooling energy was carried through as final energy.",
                    UserAction: "Provide cooling COP for final energy conversion.",
                    ClosedV1Gate: "HVAC.SYSTEM_ENERGY.SIMPLIFIED"),

                new(
                    Code: "EquipmentSizing.InvalidSafetyFactor",
                    Severity: "Error",
                    Category: "EquipmentSizing",
                    UserMessage: "Safety factor must be greater than zero.",
                    UserAction: "Enter a positive safety factor.",
                    ClosedV1Gate: "HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN"),

                new(
                    Code: "EquipmentSizing.NoEquipmentFound",
                    Severity: "Warning",
                    Category: "EquipmentSizing",
                    UserMessage: "No equipment candidates were supplied.",
                    UserAction: "Add equipment candidates before expecting recommendations.",
                    ClosedV1Gate: "HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN"),

                new(
                    Code: "EquipmentSizing.NoRecommendedEquipment",
                    Severity: "Warning",
                    Category: "EquipmentSizing",
                    UserMessage: "No equipment candidate satisfied the sizing requirements.",
                    UserAction: "Review required load, safety factor or equipment catalog.",
                    ClosedV1Gate: "HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN"),

                new(
                    Code: "Aggregation.InvalidRoomArea",
                    Severity: "Error",
                    Category: "Aggregation",
                    UserMessage: "Room area must not be negative.",
                    UserAction: "Correct room geometry before aggregation.",
                    ClosedV1Gate: "HVAC.AGGREGATION.LOAD_SUMMARY"),

                new(
                    Code: "Aggregation.NoRooms",
                    Severity: "Warning",
                    Category: "Aggregation",
                    UserMessage: "No rooms were supplied for load aggregation.",
                    UserAction: "Check room assignment to building, floor or thermal zone.",
                    ClosedV1Gate: "HVAC.AGGREGATION.LOAD_SUMMARY"),

                new(
                    Code: "Aggregation.HourlyUnavailable",
                    Severity: "Warning",
                    Category: "Aggregation",
                    UserMessage: "Hourly aggregation is not available; design-point aggregation was used.",
                    UserAction: "Provide hourly profiles for coincident hourly aggregation.",
                    ClosedV1Gate: "HVAC.AGGREGATION.LOAD_SUMMARY"),

                new(
                    Code: "Transmission.MissingBoundaryTemperature",
                    Severity: "Error",
                    Category: "Transmission",
                    UserMessage: "Boundary temperature is required for this transmission element.",
                    UserAction: "Provide adjacent, boundary or outdoor temperature for the element.",
                    ClosedV1Gate: "HVAC.ADJACENT_ZONE.SIMPLIFIED")
            ]);

        return Result<EngineeringCoreV1DiagnosticsCatalogResponse>.Success(response);
    }


    private static IReadOnlyList<EngineeringCoreV1GateStatus> FormulaGates() =>
    [
        new(
            CalculationId: "HVAC.TRANSMISSION.SIMPLE_UA",
            Name: "Transmission heat transfer",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Steady-state component heat transfer.",
            Limitation: "Does not claim full dynamic ISO 52016 node/matrix heat-balance equivalence."),

        new(
            CalculationId: "HVAC.VENTILATION.SENSIBLE_AIRFLOW",
            Name: "Ventilation and infiltration sensible load",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Sensible-only airflow heat transfer.",
            Limitation: "Does not include latent load or moisture balance in v1."),

        new(
            CalculationId: "HVAC.INTERNAL_GAINS.SENSIBLE",
            Name: "Internal sensible gains",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "People, lighting, equipment, process and custom sensible gains.",
            Limitation: "Latent gains are not part of the v1 sensible HVAC energy path."),

        new(
            CalculationId: "HVAC.WINDOW_SOLAR.SIMPLE_SHGC",
            Name: "Window solar gains",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Simplified SHGC/shading based window solar gain.",
            Limitation: "Does not claim full optical glazing or EnergyPlus solar distribution equivalence."),

        new(
            CalculationId: "HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC",
            Name: "Solar position and isotropic surface irradiance",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "ISO52010-inspired solar geometry and isotropic sky transposition.",
            Limitation: "Does not claim anisotropic/Perez sky or EnergyPlus solar transposition equivalence."),

        new(
            CalculationId: "HVAC.ROOM_LOAD.DESIGN_POINT",
            Name: "Room design-point heating and cooling load",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Design-point heating/cooling component aggregation.",
            Limitation: "Does not claim full ISO 52016 hourly dynamic zone solver equivalence."),

        new(
            CalculationId: "HVAC.AGGREGATION.LOAD_SUMMARY",
            Name: "Load aggregation",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Room to thermal zone/floor/building aggregation with optional coincident hourly peak.",
            Limitation: "Does not claim coupled multi-zone heat-transfer solver equivalence."),

        new(
            CalculationId: "HVAC.ANNUAL_ENERGY.HOURLY_KWH",
            Name: "Annual and monthly energy balance from hourly records",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Hourly-to-monthly and hourly-to-annual kWh integration from true hourly 8760 records.",
            Limitation: "Does not claim full ISO 52016 node/matrix solver or EnergyPlus comparison workflow."),

        new(
            CalculationId: "WEATHER.EPW_8760",
            Name: "EPW hourly weather import and normalization",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Normalized 8760 hourly EPW weather import gate.",
            Limitation: "Does not claim external EPW provider accuracy validation."),

        new(
            CalculationId: "WEATHER.PVGIS_8760",
            Name: "PVGIS TMY hourly weather import and normalization",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Normalized 8760 hourly PVGIS weather import gate.",
            Limitation: "Does not claim external PVGIS service accuracy validation."),

        new(
            CalculationId: "HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC",
            Name: "Simplified hourly heat balance",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "ISO52016-inspired simplified hourly RC / quasi-implicit heat-balance model.",
            Limitation: "Does not claim full ISO 52016 node/matrix solver equivalence."),

        new(
            CalculationId: "HVAC.THERMAL_ZONE.SINGLE_ZONE",
            Name: "Single thermal zone calculation path",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Single-zone engineering path with assigned-room-only aggregation and no double-counting.",
            Limitation: "Does not claim coupled multi-zone heat exchange equivalence."),

        new(
            CalculationId: "HVAC.GROUND.SIMPLIFIED",
            Name: "Simplified ground heat transfer",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "ISO13370-inspired simplified ground model using equivalent U/H values and boundary weights.",
            Limitation: "Does not claim full ISO 13370 implementation."),

        new(
            CalculationId: "HVAC.ADJACENT_ZONE.SIMPLIFIED",
            Name: "Simplified adjacent zone boundary handling",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "Simplified adjacent conditioned/unconditioned boundary heat transfer.",
            Limitation: "Does not claim coupled multi-zone solver equivalence."),

        new(
            CalculationId: "HVAC.DHW.SIMPLIFIED",
            Name: "Simplified domestic hot water demand",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "DHW demand by water volume, temperature lift and configured losses.",
            Limitation: "Does not claim full EN 12831-3 tapping profile or distribution system equivalence."),

        new(
            CalculationId: "HVAC.SYSTEM_ENERGY.SIMPLIFIED",
            Name: "Simplified system final and primary energy",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "Final/primary energy conversion using efficiency, COP and primary factor.",
            Limitation: "Does not claim full EN 15316 generation/distribution/storage/emission chain."),

        new(
            CalculationId: "HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN",
            Name: "Equipment sizing by capacity margin",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "Capacity sizing by required load, safety factor and deterministic margin ranking.",
            Limitation: "Does not claim part-load performance, manufacturer curves or detailed HVAC plant operation.")
    ];
}
