using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundBoundaryTemperatureLookup(
    IReadOnlyDictionary<string, IReadOnlyList<double>> HourlyGroundTemperaturesBySurfaceId,
    IReadOnlyDictionary<string, IReadOnlyList<double>> MonthlyGroundTemperaturesBySurfaceId,
    IReadOnlyDictionary<string, double> RepresentativeGroundTemperatureBySurfaceId,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
