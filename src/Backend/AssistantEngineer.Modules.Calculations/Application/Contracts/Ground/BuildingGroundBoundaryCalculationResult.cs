using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record BuildingGroundBoundaryCalculationResult(
    string BuildingId,
    IReadOnlyList<GroundSurfaceBoundaryCalculationResult> GroundSurfaces,
    IReadOnlyDictionary<string, double> SurfaceHeatTransferCoefficientsWPerKelvin,
    IReadOnlyDictionary<string, IReadOnlyList<double>> SurfaceHourlyGroundTemperaturesCelsius,
    IReadOnlyDictionary<string, IReadOnlyList<double>> SurfaceMonthlyGroundTemperaturesCelsius,
    double TotalGroundHeatTransferCoefficientWPerKelvin,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
