using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundSoilProperties(
    double ConductivityWPerMeterKelvin,
    double? DensityKgPerCubicMeter,
    double? SpecificHeatJPerKgKelvin,
    double? ThermalDiffusivitySquareMetersPerSecond,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
