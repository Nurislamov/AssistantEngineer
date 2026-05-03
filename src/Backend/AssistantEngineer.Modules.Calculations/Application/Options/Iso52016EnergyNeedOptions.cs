namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso52016EnergyNeedOptions
{
    public int DefaultWeatherYear { get; init; } = 2020;
    public double DefaultHeatingSetbackC { get; init; } = 17.0;
    public double DefaultCoolingSetpointC { get; init; } = 26.0;
    public double DefaultCoolingSetbackC { get; init; } = 30.0;
    public double DefaultAirChangesPerHour { get; init; } = 0.5;
    public double AirHeatCapacityWhPerM3K { get; init; } = 0.34;
    public double InternalHeatCapacityJPerM2K { get; init; } = 10_000.0;
    public double DefaultSolarUtilizationFactor { get; init; } = 0.75;
    public double DefaultWindowFrameAreaFraction { get; init; } = 0.25;
    public double DefaultDirectSolarShadingReductionFactor { get; init; } = 1.0;
    public double DefaultOverhangDepthM { get; init; } = 0;
    public double DefaultSideFinDepthM { get; init; } = 0;
    public double DefaultWindowRevealDepthM { get; init; } = 0;
    public double DefaultWindowHeightM { get; init; } = 1.5;
    public double DefaultWindowWidthM { get; init; } = 1.5;
    public double MinimumDirectSolarShadingReductionFactor { get; init; } = 0.15;
    public double DiffuseSolarShareUnaffectedByShading { get; init; } = 0.3;
    public double LatitudeDegrees { get; init; } = 41.0;
    public double LongitudeDegrees { get; init; } = 69.0;
    public double TimeZoneOffsetHours { get; init; } = 5.0;
    public double GroundReflectance { get; init; } = 0.2;

    public double DefaultGroundBoundaryTemperatureC { get; init; } = 12.0;
    public double AdjacentUnconditionedTemperatureWeight { get; init; } = 0.5;
    public bool TreatSameUseAdjacentConditionedAsAdiabatic { get; init; } = true;
}
