using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundClimateInput(
    IReadOnlyList<double>? MonthlyOutdoorTemperaturesCelsius,
    IReadOnlyList<double>? HourlyOutdoorTemperaturesCelsius,
    double? AnnualMeanOutdoorTemperatureCelsius,
    double? GroundTemperatureAmplitudeCelsius,
    double? GroundTemperaturePhaseShiftDays,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
