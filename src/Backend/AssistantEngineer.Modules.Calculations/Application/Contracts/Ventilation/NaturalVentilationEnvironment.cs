using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationEnvironment(
    double IndoorTemperatureCelsius,
    double OutdoorTemperatureCelsius,
    double WindSpeedMetersPerSecond,
    double? WindSpeedHeightMeters,
    double? OpeningReferenceHeightMeters,
    double? OutdoorAirDensityKgPerCubicMeter,
    double? IndoorAirDensityKgPerCubicMeter,
    double? AtmosphericPressurePa,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
