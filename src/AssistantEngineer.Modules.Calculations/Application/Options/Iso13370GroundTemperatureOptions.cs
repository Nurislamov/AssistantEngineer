namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso13370GroundTemperatureOptions
{
    public double MeanTemperatureOffsetC { get; init; } = 1.5;
    public double AmplitudeAttenuationFactor { get; init; } = 0.55;
    public int PhaseShiftDays { get; init; } = 45;
    public double MinimumGroundTemperatureC { get; init; } = 4.0;
    public double MaximumGroundTemperatureC { get; init; } = 24.0;
}