using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record ThermalZoneGroundBoundaryInputAdapterResult(
    double? RepresentativeBuildingGroundTemperatureCelsius,
    IReadOnlyDictionary<string, double> RepresentativeGroundTemperatureBySurfaceId,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
