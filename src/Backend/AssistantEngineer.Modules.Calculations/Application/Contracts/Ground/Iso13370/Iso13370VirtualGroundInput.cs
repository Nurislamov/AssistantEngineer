namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370VirtualGroundInput(
    SlabOnGroundGeometry Geometry,
    GroundThermalProperties GroundThermalProperties,
    double AnnualAverageOutdoorTemperatureC,
    IReadOnlyList<double>? MonthlyOutdoorTemperatureProfileC,
    double SeasonalAmplitudeC,
    double SeasonalPhaseShiftMonths,
    double IndoorSetpointTemperatureC,
    GroundThermalBridgeInput? ThermalBridge,
    Iso13370GroundCalculationOptions? Options);

