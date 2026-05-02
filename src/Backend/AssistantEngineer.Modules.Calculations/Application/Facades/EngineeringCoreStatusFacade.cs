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
                "No exact pyBuildingEnergy numerical parity claim.",
                "No exact EnergyPlus numerical parity claim.",
                "No ASHRAE 140 validation coverage claim.",
                "No full ISO 52016 node/matrix solver parity claim.",
                "No full ISO 52010 climate conversion parity claim.",
                "No full ISO 13370 implementation claim.",
                "No full EN 15316 generation/distribution/storage/emission chain claim.",
                "No full coupled multi-zone heat-balance simulation claim.",
                "No latent/moisture/humidity calculation claim."
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

    private static IReadOnlyList<EngineeringCoreV1GateStatus> FormulaGates() =>
    [
        new(
            CalculationId: "HVAC.TRANSMISSION.SIMPLE_UA",
            Name: "Transmission heat transfer",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Steady-state component heat transfer.",
            Limitation: "Does not claim full dynamic ISO 52016 node/matrix heat-balance parity."),

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
            Limitation: "Does not claim full optical glazing or EnergyPlus solar distribution parity."),

        new(
            CalculationId: "HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC",
            Name: "Solar position and isotropic surface irradiance",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "ISO52010-inspired solar geometry and isotropic sky transposition.",
            Limitation: "Does not claim anisotropic/Perez sky or EnergyPlus solar transposition parity."),

        new(
            CalculationId: "HVAC.ROOM_LOAD.DESIGN_POINT",
            Name: "Room design-point heating and cooling load",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Design-point heating/cooling component aggregation.",
            Limitation: "Does not claim full ISO 52016 hourly dynamic zone solver parity."),

        new(
            CalculationId: "HVAC.AGGREGATION.LOAD_SUMMARY",
            Name: "Load aggregation",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Room to thermal zone/floor/building aggregation with optional coincident hourly peak.",
            Limitation: "Does not claim coupled multi-zone heat-transfer solver parity."),

        new(
            CalculationId: "HVAC.ANNUAL_ENERGY.HOURLY_KWH",
            Name: "Annual and monthly energy balance from hourly records",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Hourly-to-monthly and hourly-to-annual kWh integration from true hourly 8760 records.",
            Limitation: "Does not claim full ISO 52016 node/matrix solver or EnergyPlus parity."),

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
            Limitation: "Does not claim full ISO 52016 node/matrix solver parity."),

        new(
            CalculationId: "HVAC.THERMAL_ZONE.SINGLE_ZONE",
            Name: "Single thermal zone calculation path",
            Status: ClosedV1,
            Priority: "P0",
            Scope: "Single-zone engineering path with assigned-room-only aggregation and no double-counting.",
            Limitation: "Does not claim coupled multi-zone heat exchange parity."),

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
            Limitation: "Does not claim coupled multi-zone solver parity."),

        new(
            CalculationId: "HVAC.DHW.SIMPLIFIED",
            Name: "Simplified domestic hot water demand",
            Status: ClosedV1,
            Priority: "P1",
            Scope: "DHW demand by water volume, temperature lift and configured losses.",
            Limitation: "Does not claim full EN 12831-3 tapping profile or distribution system parity."),

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