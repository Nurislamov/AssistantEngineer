namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundTemperatureProfileRequest(
    GroundProfileTimeResolution TimeResolution,
    GroundTemperatureProfileMode Mode,
    IReadOnlyList<double>? OutdoorTemperatureProfileCelsius,
    double? AnnualMeanOutdoorTemperatureCelsius,
    double GroundAnnualMeanTemperatureCelsius,
    double GroundTemperatureAmplitudeCelsius,
    double? GroundTemperaturePhaseShiftDays,
    int NumberOfSteps,
    double TimeStepHours,
    int? ColdestMonthIndex = null);
