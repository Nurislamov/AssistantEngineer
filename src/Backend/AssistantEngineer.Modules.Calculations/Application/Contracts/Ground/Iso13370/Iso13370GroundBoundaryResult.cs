namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370GroundBoundaryResult(
    double CharacteristicDimensionM,
    double EquivalentGroundUValueWPerM2K,
    double HeatTransferCoefficientWPerK,
    double GroundWeight,
    double OutdoorWeight,
    double IndoorWeight,
    IReadOnlyList<double> MonthlyBoundaryTemperaturesC,
    double AnnualMeanBoundaryTemperatureC,
    Iso13370GroundBoundaryTemperatureProfile TemperatureProfile,
    IReadOnlyList<Iso13370GroundBoundaryDiagnostics> Diagnostics);
