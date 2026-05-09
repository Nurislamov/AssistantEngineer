using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundTemperatureProfileResult(
    IReadOnlyList<double> MonthlyGroundBoundaryTemperaturesCelsius,
    IReadOnlyList<double> HourlyGroundBoundaryTemperaturesCelsius,
    IReadOnlyList<double> MonthlyOutdoorTemperaturesCelsius,
    double AnnualMeanOutdoorTemperatureCelsius,
    double GroundTemperatureAmplitudeCelsius,
    double GroundTemperaturePhaseShiftDays,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<double>? GroundTemperatureProfileCelsius = null,
    IReadOnlyList<string>? Assumptions = null,
    IReadOnlyList<string>? Warnings = null,
    GroundTemperatureProfileMode? Mode = null,
    GroundProfileTimeResolution? TimeResolution = null);
