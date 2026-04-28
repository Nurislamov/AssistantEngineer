namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso13370GroundHeatTransferOptions
{
    public double GroundConductivityWPerMK { get; init; } = 2.0;
    public double BaseCharacteristicDepthM { get; init; } = 1.5;
    public double PerimeterAmplificationFactor { get; init; } = 0.9;

    public double SlabOnGroundFactor { get; init; } = 1.0;
    public double BasementConditionedFactor { get; init; } = 0.65;
    public double BasementUnconditionedFactor { get; init; } = 0.80;
    public double CrawlSpaceFactor { get; init; } = 0.75;
    public double VentilatedCrawlSpaceFactor { get; init; } = 0.95;
}