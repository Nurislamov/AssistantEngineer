namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso13370GroundHeatTransferOptions
{
    public bool UseIso13370InspiredBoundaryCalculator { get; init; } = false;
    public bool UseIso13370VirtualGroundTemperatureLane { get; init; } = false;

    public double GroundConductivityWPerMK { get; init; } = 2.0;
    public double BaseCharacteristicDepthM { get; init; } = 1.5;
    public double PerimeterAmplificationFactor { get; init; } = 0.9;

    public double IndoorAnnualMeanTemperatureC { get; init; } = 20.0;
    public double OutdoorAnnualMeanTemperatureC { get; init; } = 10.0;
    public double GroundAnnualMeanTemperatureC { get; init; } = 12.0;
    public double GroundTemperatureAmplitudeC { get; init; } = 4.0;
    public double GroundTemperaturePhaseShiftMonths { get; init; } = 1.0;
    public double VirtualGroundSeasonalAmplitudeC { get; init; } = 8.0;
    public double VirtualGroundSeasonalPhaseShiftMonths { get; init; } = 1.0;
    public double VirtualGroundEquivalentGroundThicknessM { get; init; } = 0.0;
    public bool UseIso13370VirtualGroundPerimeterThermalBridge { get; init; } = false;
    public double VirtualGroundThermalBridgeLinearTransmittanceWPerMK { get; init; } = 0.0;
    public bool VirtualGroundEnableSeasonalComponent { get; init; } = true;
    public bool VirtualGroundEnablePerimeterThermalBridge { get; init; } = true;
    public double VirtualGroundSeasonalAttenuationFactor { get; init; } = 0.55;
    public double VirtualGroundMonthlyHeatTransferVariationFactor { get; init; } = 0.05;
    public double VirtualGroundMinimumEquivalentGroundThicknessM { get; init; } = 0.3;
    public double VirtualGroundMaximumEquivalentGroundThicknessM { get; init; } = 8.0;

    public double SlabOnGroundFactor { get; init; } = 1.0;
    public double BasementConditionedFactor { get; init; } = 0.65;
    public double BasementUnconditionedFactor { get; init; } = 0.80;
    public double CrawlSpaceFactor { get; init; } = 0.75;
    public double VentilatedCrawlSpaceFactor { get; init; } = 0.95;
}
