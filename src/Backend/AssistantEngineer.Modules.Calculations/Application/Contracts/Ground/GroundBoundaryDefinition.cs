namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundBoundaryDefinition(
    string BoundaryId,
    string ZoneId,
    GroundBoundaryType BoundaryType,
    double AreaSquareMeters,
    double? ExposedPerimeterMeters,
    double? ThermalTransmittanceUValueWPerSquareMeterKelvin,
    double? FloorDepthBelowGradeMeters,
    double? WallHeightBelowGradeMeters,
    double? CharacteristicDimensionMeters,
    double SoilThermalConductivityWPerMeterKelvin,
    double GroundAnnualMeanTemperatureCelsius,
    double GroundTemperatureAmplitudeCelsius,
    double? GroundTemperaturePhaseShiftDays,
    int? ColdestMonthIndex,
    double? EdgeInsulationThicknessMeters,
    double? EdgeInsulationConductivityWPerMeterKelvin,
    GroundBoundaryCalculationMode CalculationMode,
    string? ThermalBoundaryId = null,
    string? AdjacentZoneId = null);
